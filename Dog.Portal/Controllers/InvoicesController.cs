using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Portal.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ICurrentUserInfoService _currentUserService;
        private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

        private readonly Dog.Application.Contracts.IFileStorageService _storage;

        public InvoicesController(IApiClient apiClient, ICurrentUserInfoService currentUserService, Dog.Application.Contracts.IFileStorageService storage)
        {
            _apiClient = apiClient;
            _currentUserService = currentUserService;
            _storage = storage;
        }

        // ── PROOF OF PAYMENT (screenshot / slip / PDF) — stored, and its URL is saved on the payment ──
        [HttpPost]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<IActionResult> UploadProof(IFormFile file)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            var p = Dog.Portal.Helpers.PermHelper.For(HttpContext);
            if (!p.Can("invoice.pay") && !p.Can("training.payment.record"))
                return Json(new { success = false, message = "You do not have permission to do this." });
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file selected." });
            var ext = (Path.GetExtension(file.FileName) ?? "").ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".pdf"))
                return Json(new { success = false, message = "Only JPG, PNG, WebP or PDF files are allowed." });
            if (file.Length > 12 * 1024 * 1024) return Json(new { success = false, message = "That file is too large (limit 12 MB)." });
            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;
                string url;
                if (_storage.IsEnabled) url = await _storage.UploadBytesAsync(ms.ToArray(), file.FileName, contentType, "payment-proofs");
                else
                {
                    var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "payment-proofs");
                    Directory.CreateDirectory(dir);
                    var stored = $"{Guid.NewGuid()}{ext}";
                    await System.IO.File.WriteAllBytesAsync(Path.Combine(dir, stored), ms.ToArray());
                    url = $"/uploads/payment-proofs/{stored}";
                }
                return Json(new { success = true, url });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Upload failed: {ex.Message}" }); }
        }

        private async Task<bool> IsAuthenticated() => await _currentUserService.IsAuthenticated();

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        // ── BRANDED PDF ───────────────────────────────────────────────────────
        // GET /Invoices/Pdf/{id} — QuestPDF-generated invoice with company logo + bill-to.
        public async Task<IActionResult> Pdf(int id)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");
            try
            {
                var invResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Invoice/{id}");
                if (invResp?.Success != true || invResp.Data == null)
                    return NotFound(invResp?.Message ?? "Invoice not found.");

                var inv = JsonSerializer.Deserialize<Dog.Application.Features.Invoices.DTOs.InvoiceDto>(JsonSerializer.Serialize(invResp.Data), Opts);
                if (inv == null) return NotFound("Invoice not found.");

                // Company branding (optional — never blocks the PDF)
                Dog.Application.Features.Companies.DTOs.CompanyDto? company = null;
                try
                {
                    var companyId = inv.CompanyId > 0 ? inv.CompanyId : await _currentUserService.GetActiveCompanyId();
                    var compResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/companies/{companyId}");
                    if (compResp?.Data != null)
                        company = JsonSerializer.Deserialize<Dog.Application.Features.Companies.DTOs.CompanyDto>(JsonSerializer.Serialize(compResp.Data), Opts);
                }
                catch { /* branding is optional */ }

                var logo = await TryLoadLogoAsync(company?.Logo);
                Dog.Application.Features.BillingSetup.PdfInfoDto? info = null;
                try
                {
                    var infoResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/BillingSetup/pdf-info?CompanyId={inv.CompanyId}");
                    if (infoResp?.Data != null) info = JsonSerializer.Deserialize<Dog.Application.Features.BillingSetup.PdfInfoDto>(JsonSerializer.Serialize(infoResp.Data), Opts);
                }
                catch { /* tax profile and bank details are optional on the printout */ }
                var pdf = Dog.Application.Common.Pdf.InvoicePdfBuilder.Build(inv, company, logo, info?.Profile, info?.Banks);
                return File(pdf, "application/pdf", $"Invoice_{inv.InvoiceNumber}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to generate invoice PDF: {ex.Message}");
            }
        }

        /// <summary>Best-effort logo fetch. Absolute URLs are downloaded; anything else / any failure → null.</summary>
        private Task<byte[]?> TryLoadLogoAsync(string? logo)
            => Dog.Portal.Services.PdfImageLoader.LoadAsync(logo, $"{Request.Scheme}://{Request.Host}");

        // ── INDEX ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 15)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new InvoiceListViewModel { SearchTerm = search, StatusFilter = status, Page = page, PageSize = pageSize };

            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();

                // Load invoices
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Invoice?CompanyId={companyId}");
                if (resp?.Success == true && resp.Data != null)
                {
                    var json = JsonSerializer.Serialize(resp.Data);
                    var invoices = JsonSerializer.Deserialize<List<InvoiceItemViewModel>>(json, Opts) ?? new();

                    // Stats
                    vm.DraftCount = invoices.Count(i => i.Status == "DRAFT");
                    vm.SentCount = invoices.Count(i => i.Status == "SENT");
                    vm.PartialCount = invoices.Count(i => i.Status == "PARTIAL");
                    vm.PaidCount = invoices.Count(i => i.Status == "PAID");
                    vm.TotalOutstanding = invoices.Where(i => i.Status != "PAID" && i.Status != "CANCELLED").Sum(i => i.BalanceAmount);
                    vm.TotalPaid = invoices.Sum(i => i.PaidAmount);

                    // Filter
                    if (!string.IsNullOrWhiteSpace(search))
                        invoices = invoices.Where(i =>
                            (i.InvoiceNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                            (i.CustomerName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)).ToList();

                    if (!string.IsNullOrWhiteSpace(status))
                        invoices = invoices.Where(i => i.Status == status.ToUpper()).ToList();

                    vm.TotalCount = invoices.Count;
                    vm.InvoiceList = invoices.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                }

                // Load customers for dropdown
                try
                {
                    var custResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Customer?CompanyId={companyId}");
                    if (custResp?.Success == true && custResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(custResp.Data);
                        vm.Customers = JsonSerializer.Deserialize<List<CustomerDropdown>>(json, Opts) ?? new();
                    }
                }
                catch { }

                // Load dogs — invoice line items can be linked to one of our dogs
                try
                {
                    var dogsResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Animal/GetPets?CompanyId={companyId}");
                    if (dogsResp?.Success == true && dogsResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(dogsResp.Data);
                        vm.Dogs = JsonSerializer.Deserialize<List<AnimalListDto>>(json, Opts) ?? new();
                    }
                }
                catch { }

                // Load pending suspense
                try
                {
                    var suspResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Invoice/suspense?CompanyId={companyId}");
                    if (suspResp?.Success == true && suspResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(suspResp.Data);
                        var all = JsonSerializer.Deserialize<List<SuspenseItemViewModel>>(json, Opts) ?? new();
                        vm.PendingSuspense = all.Where(s => s.Status == "PENDING")
                            .Select(s => new SuspenseDropdown { Id = s.Id, CustomerName = s.CustomerName, Amount = s.Amount, CurrencyCode = s.CurrencyCode })
                            .ToList();
                    }
                }
                catch { }

                // Load unclaimed bank transactions
                try
                {
                    var btResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/BankTransaction?CompanyId={companyId}");
                    if (btResp?.Success == true && btResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(btResp.Data);
                        var all = JsonSerializer.Deserialize<List<BankTxDropdown>>(json, Opts) ?? new();
                        vm.UnclaimedBankTx = all;
                    }
                }
                catch { }

                // Load currencies
                try
                {
                    var curResp = await _apiClient.GetAsync<ApiResponse<object>>("api/Currency");
                    if (curResp?.Success == true && curResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(curResp.Data);
                        vm.Currencies = JsonSerializer.Deserialize<List<CurrencyDropdown>>(json, Opts) ?? new();
                    }
                }
                catch { }

                // Load banks for bank payment dropdown
                try
                {
                    var banksResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Bank?CompanyId={companyId}");
                    if (banksResp?.Success == true && banksResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(banksResp.Data);
                        vm.Banks = JsonSerializer.Deserialize<List<BankDropdown>>(json, Opts) ?? new();
                    }
                }
                catch { }

                // Load bank accounts (with currency info) for auto-populate
                try
                {
                    var accResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/BankAccount/GetAccountByCompany?CompanyId={companyId}");
                    if (accResp?.Success == true && accResp.Data != null)
                    {
                        var json = JsonSerializer.Serialize(accResp.Data);
                        vm.BankAccounts = JsonSerializer.Deserialize<List<BankAccountDropdown>>(json, Opts) ?? new();
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unable to load invoices: {ex.Message}";
            }

            return View(vm);
        }

        // ── SUSPENSE PAGE ─────────────────────────────────────────────────────
        public async Task<IActionResult> Suspense(string? search, int page = 1, int pageSize = 15)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");
            var vm = new SuspenseListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Invoice/suspense?CompanyId={companyId}");
                if (resp?.Success == true && resp.Data != null)
                {
                    var json = JsonSerializer.Serialize(resp.Data);
                    var list = JsonSerializer.Deserialize<List<SuspenseItemViewModel>>(json, Opts) ?? new();
                    if (!string.IsNullOrWhiteSpace(search))
                        list = list.Where(s => s.CustomerName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true).ToList();
                    vm.TotalCount = list.Count;
                    vm.SuspenseList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                }

                // Invoices for apply-suspense dropdown
                var invResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Invoice?CompanyId={companyId}");
                if (invResp?.Success == true && invResp.Data != null)
                {
                    var json = JsonSerializer.Serialize(invResp.Data);
                    var invs = JsonSerializer.Deserialize<List<InvoiceItemViewModel>>(json, Opts) ?? new();
                    vm.Invoices = invs.Where(i => i.Status != "PAID" && i.Status != "CANCELLED")
                        .Select(i => new InvoiceDropdown { Id = i.Id, InvoiceNumber = i.InvoiceNumber, BalanceAmount = i.BalanceAmount, CustomerName = i.CustomerName })
                        .ToList();
                }
            }
            catch (Exception ex) { ViewBag.Error = $"Unable to load suspense: {ex.Message}"; }
            return View(vm);
        }

        // ── RECEIPTS PAGE ─────────────────────────────────────────────────────
        public async Task<IActionResult> Receipts(string? search, int page = 1, int pageSize = 15)
        {
            if (!await IsAuthenticated()) return RedirectToAction("Login", "Auth");
            var vm = new ReceiptListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Invoice/receipts?CompanyId={companyId}");
                if (resp?.Success == true && resp.Data != null)
                {
                    var json = JsonSerializer.Serialize(resp.Data);
                    var list = JsonSerializer.Deserialize<List<ReceiptItemViewModel>>(json, Opts) ?? new();
                    if (!string.IsNullOrWhiteSpace(search))
                        list = list.Where(r =>
                            (r.ReceiptNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                            (r.CustomerName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                            (r.InvoiceNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)).ToList();
                    vm.TotalCount = list.Count;
                    vm.ReceiptList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                }
            }
            catch (Exception ex) { ViewBag.Error = $"Unable to load receipts: {ex.Message}"; }
            return View(vm);
        }

        // ── AJAX ACTIONS ──────────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new
                {
                    companyId,
                    customerId = req.CustomerId,
                    customerName = req.CustomerName,
                    customerEmail = req.CustomerEmail,
                    customerPhone = req.CustomerPhone,
                    invoiceDate = req.InvoiceDate,
                    dueDate = req.DueDate,
                    taxAmount = req.TaxAmount,
                    discountAmount = req.DiscountAmount,
                    notes = req.Notes,
                    terms = req.Terms,
                    items = req.Items.Select(i => new { i.Description, i.Quantity, i.UnitPrice, i.TaxRate, i.DiscountRate })
                };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Invoice", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Send(int id)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<object>>($"api/Invoice/{id}/send", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Sent." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<object>>($"api/Invoice/{id}/cancel", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Cancelled." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<object>>($"api/Invoice/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new { req.InvoiceId, companyId, req.Amount, req.PaymentMethod, req.ReferenceNumber, req.Notes, paymentDate = DateTime.UtcNow };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Invoice/pay", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Payment recorded." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitBankPayment([FromBody] SubmitBankPaymentRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new
                {
                    req.InvoiceId,
                    companyId,
                    req.CustomerId,
                    req.BankId,
                    req.CurrencyId,
                    req.Amount,
                    req.AccountNumber,
                    req.StatementReference,
                    req.Description,
                    transactionDate = DateTime.UtcNow,
                    req.PopFileBase64,
                    req.PopFileName
                };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Invoice/submit-bank-payment", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Bank payment submitted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> ClaimBankTransaction([FromBody] ClaimBankTxRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new { req.BankTransactionId, req.InvoiceId, companyId, req.CustomerId, req.CurrencyId };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Invoice/claim-bank", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Claimed." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> ApplySuspense([FromBody] ApplySuspenseRequest req)
        {
            if (!await IsAuthenticated()) return Json(new { success = false, message = "Not authenticated" });
            try
            {
                var companyId = await _currentUserService.GetActiveCompanyId();
                var dto = new { req.SuspenseId, req.InvoiceId, companyId, req.Amount, req.ExchangeRateId };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/Invoice/apply-suspense", dto);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Applied." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}
