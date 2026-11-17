using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Invoices.Commands;
using Dog.Application.Features.Invoices.DTOs;
using Dog.Application.Features.Invoices.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Invoices.Handlers
{
    // ── GET ALL INVOICES ──────────────────────────────────────────────────────
    public class GetInvoicesByCompanyHandler : IRequestHandler<GetInvoicesByCompanyQuery, ApiResponse<List<InvoiceDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetInvoicesByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<InvoiceDto>>> Handle(GetInvoicesByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Invoice>()
                .TableNoTracking
                .Where(i => i.CompanyId == request.CompanyId)
                .Include(i => i.Items)
                .Include(i => i.Ledger)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(ct);

            return ApiResponse<List<InvoiceDto>>.Ok(list.Select(InvoiceMapper.Map).ToList());
        }
    }

    // ── GET INVOICE BY ID ─────────────────────────────────────────────────────
    public class GetInvoiceByIdHandler : IRequestHandler<GetInvoiceByIdQuery, ApiResponse<InvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetInvoiceByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<InvoiceDto>> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
        {
            var inv = await _uow.Repository<Invoice>()
                .TableNoTracking
                .Include(i => i.Items)
                .Include(i => i.Ledger)
                .FirstOrDefaultAsync(i => i.Id == request.Id, ct);
            if (inv == null) return ApiResponse<InvoiceDto>.Fail("Invoice not found.");
            var dto = InvoiceMapper.Map(inv);
            var banks = await Billing.InvoicePaymentSupport.BankAccountLabelsAsync(_uow, inv.Ledger.Where(p => p.DestinationBankAccountId != null).Select(p => p.DestinationBankAccountId!.Value), ct);
            dto.Payments = inv.Ledger.OrderBy(p => p.CreatedAt).Select(p => Billing.InvoicePaymentSupport.Map(p, inv, banks)).ToList();
            return ApiResponse<InvoiceDto>.Ok(dto);
        }
    }

    internal static class InvoiceBuilder
    {
        public static List<InvoiceItem> BuildItems(IEnumerable<CreateInvoiceItemDto> src) => src.Where(i => !string.IsNullOrWhiteSpace(i.Description)).Select(i => new InvoiceItem
        {
            Description  = i.Description.Trim(),
            Quantity     = i.Quantity,
            UnitPrice    = i.UnitPrice,
            TaxRate      = i.TaxRate,
            DiscountRate = i.DiscountRate,
            Amount       = Math.Round(i.Quantity * i.UnitPrice * (1 - i.DiscountRate / 100) * (1 + i.TaxRate / 100), 2)
        }).ToList();

        public static string? Validate(CreateInvoiceDto d)
        {
            if (d.Items == null || !d.Items.Any(i => !string.IsNullOrWhiteSpace(i.Description))) return "Add at least one line item.";
            if (d.Items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0 || i.TaxRate < 0 || i.TaxRate > 100 || i.DiscountRate < 0 || i.DiscountRate > 100))
                return "Check the line items: quantity must be above 0, price cannot be negative, tax and discount must be 0-100%.";
            if (d.DueDate.Date < d.InvoiceDate.Date) return "The due date cannot be before the invoice date.";
            if (string.IsNullOrWhiteSpace(d.BillToName) && string.IsNullOrWhiteSpace(d.CustomerName) && d.CustomerCompanyId is not > 0) return "Choose the company to bill.";
            return null;
        }

        /// <summary>Copies the billed company's details onto the invoice (a snapshot, so later edits never change an issued invoice).</summary>
        public static async Task ApplyBillToAsync(IUnitOfWork uow, Invoice e, CreateInvoiceDto d, CancellationToken ct)
        {
            e.CustomerCompanyId = d.CustomerCompanyId is > 0 ? d.CustomerCompanyId : null;
            CustomerCompany? co = null;
            if (e.CustomerCompanyId != null)
                co = await uow.Repository<CustomerCompany>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == e.CustomerCompanyId, ct);
            if (co != null)
            {
                e.BillToName = co.Name;
                e.BillToAddress = string.Join(", ", new[] { co.Address, co.City, co.Country }.Where(x => !string.IsNullOrWhiteSpace(x)));
                e.BillToTaxNumber = co.TaxNumber; e.BillToVatNumber = co.VatNumber;
                e.BillToEmail = co.BillingEmail ?? co.Email; e.BillToPhone = co.Phone;
                e.CustomerName = co.Name;
                e.CustomerEmail = string.IsNullOrWhiteSpace(d.CustomerEmail) ? (co.BillingEmail ?? co.Email) : d.CustomerEmail;
                e.CustomerPhone = string.IsNullOrWhiteSpace(d.CustomerPhone) ? co.Phone : d.CustomerPhone;
            }
            else
            {
                e.BillToName = string.IsNullOrWhiteSpace(d.BillToName) ? d.CustomerName : d.BillToName;
                e.BillToAddress = d.BillToAddress; e.BillToTaxNumber = d.BillToTaxNumber; e.BillToVatNumber = d.BillToVatNumber;
                e.BillToEmail = d.CustomerEmail; e.BillToPhone = d.CustomerPhone;
                e.CustomerName = d.CustomerName ?? d.BillToName; e.CustomerEmail = d.CustomerEmail; e.CustomerPhone = d.CustomerPhone;
            }
            e.CustomerId = d.CustomerId;
            e.ContactPerson = d.ContactPerson;
            e.PurchaseOrderNumber = d.PurchaseOrderNumber;
            e.CurrencyCode = string.IsNullOrWhiteSpace(d.CurrencyCode) ? "USD" : d.CurrencyCode.Trim().ToUpperInvariant();
            e.IsFiscal = d.IsFiscal;
            if (d.IsFiscal)
            {
                var me = await uow.Repository<Company>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == e.CompanyId, ct);
                e.FiscalInvoiceNumber = d.FiscalInvoiceNumber; e.FiscalVerificationCode = d.FiscalVerificationCode;
                e.FiscalDate = d.FiscalDate; e.FiscalDeviceSerial = me?.FiscalDeviceSerial;
            }
            else { e.FiscalInvoiceNumber = null; e.FiscalVerificationCode = null; e.FiscalDate = null; e.FiscalDeviceSerial = null; }
        }
    }

    // ── CREATE INVOICE ────────────────────────────────────────────────────────
    public class CreateInvoiceHandler : IRequestHandler<CreateInvoiceCommand, ApiResponse<InvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateInvoiceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<InvoiceDto>> Handle(CreateInvoiceCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var err = InvoiceBuilder.Validate(d);
            if (err != null) return ApiResponse<InvoiceDto>.Fail(err);

            // Generate invoice number: INV-YYYYMMDD-XXXX (never reuse a number, even of a deleted invoice)
            var count = await _uow.Repository<Invoice>().TableNoTracking.IgnoreQueryFilters().CountAsync(ct);
            string invoiceNumber;
            do { count++; invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{count:D4}"; }
            while (await _uow.Repository<Invoice>().TableNoTracking.IgnoreQueryFilters().AnyAsync(i => i.InvoiceNumber == invoiceNumber, ct));

            var items = InvoiceBuilder.BuildItems(d.Items);
            var subTotal = items.Sum(i => i.Quantity * i.UnitPrice);
            var total    = Math.Max(0, items.Sum(i => i.Amount) - d.DiscountAmount + d.TaxAmount);

            var entity = new Invoice
            {
                InvoiceNumber = invoiceNumber, CompanyId = d.CompanyId,
                InvoiceDate = d.InvoiceDate, DueDate = d.DueDate,
                SubTotal = subTotal, TaxAmount = d.TaxAmount, DiscountAmount = d.DiscountAmount,
                TotalAmount = total, PaidAmount = 0, BalanceAmount = total, Status = "DRAFT",
                Notes = d.Notes, Terms = d.Terms, Items = items,
            };
            await InvoiceBuilder.ApplyBillToAsync(_uow, entity, d, ct);

            await _uow.Repository<Invoice>().AddAsync(entity, ct);
            return ApiResponse<InvoiceDto>.Ok(InvoiceMapper.Map(entity), $"Invoice {invoiceNumber} created.");
        }
    }

    // ── UPDATE INVOICE (a DRAFT can be fully re-built; an issued invoice only changes its notes, terms and due date) ──
    public class UpdateInvoiceHandler : IRequestHandler<UpdateInvoiceCommand, ApiResponse<InvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateInvoiceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<InvoiceDto>> Handle(UpdateInvoiceCommand request, CancellationToken ct)
        {
            var repo   = _uow.Repository<Invoice>();
            var d = request.Dto;
            var entity = await repo.Table.AsTracking().Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == d.Id, ct);
            if (entity == null) return ApiResponse<InvoiceDto>.Fail("Invoice not found.");
            if (entity.Status == "PAID") return ApiResponse<InvoiceDto>.Fail("Cannot edit a paid invoice.");
            if (entity.Status == "CANCELLED") return ApiResponse<InvoiceDto>.Fail("Cannot edit a cancelled invoice.");

            if (entity.Status == "DRAFT" && d.Items != null && d.Items.Count > 0)
            {
                var err = InvoiceBuilder.Validate(d);
                if (err != null) return ApiResponse<InvoiceDto>.Fail(err);
                foreach (var old in entity.Items.ToList()) await _uow.Repository<InvoiceItem>().DeleteAsync(old, ct);
                entity.Items.Clear();
                var items = InvoiceBuilder.BuildItems(d.Items);
                foreach (var it in items) { it.InvoiceId = entity.Id; entity.Items.Add(it); }
                entity.InvoiceDate = d.InvoiceDate;
                entity.SubTotal = items.Sum(i => i.Quantity * i.UnitPrice);
                entity.TaxAmount = d.TaxAmount; entity.DiscountAmount = d.DiscountAmount;
                entity.TotalAmount = Math.Max(0, items.Sum(i => i.Amount) - d.DiscountAmount + d.TaxAmount);
                entity.BalanceAmount = entity.TotalAmount - entity.PaidAmount;
                await InvoiceBuilder.ApplyBillToAsync(_uow, entity, d, ct);
            }
            else
            {
                entity.PurchaseOrderNumber = d.PurchaseOrderNumber ?? entity.PurchaseOrderNumber;
                entity.ContactPerson = d.ContactPerson ?? entity.ContactPerson;
            }
            entity.DueDate = d.DueDate;
            entity.Notes   = d.Notes;
            entity.Terms   = d.Terms;

            await repo.UpdateAsync(entity, ct);
            return ApiResponse<InvoiceDto>.Ok(InvoiceMapper.Map(entity), "Invoice updated.");
        }
    }

    // ── DELETE INVOICE ────────────────────────────────────────────────────────
    public class DeleteInvoiceHandler : IRequestHandler<DeleteInvoiceCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteInvoiceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteInvoiceCommand request, CancellationToken ct)
        {
            var repo   = _uow.Repository<Invoice>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Invoice not found.");
            if (entity.Status == "PAID") return ApiResponse<bool>.Fail("Cannot delete a paid invoice.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Invoice deleted.");
        }
    }

    // ── SEND INVOICE (DRAFT → SENT) ───────────────────────────────────────────
    public class SendInvoiceHandler : IRequestHandler<SendInvoiceCommand, ApiResponse<InvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public SendInvoiceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<InvoiceDto>> Handle(SendInvoiceCommand request, CancellationToken ct)
        {
            var repo   = _uow.Repository<Invoice>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<InvoiceDto>.Fail("Invoice not found.");
            if (entity.Status != "DRAFT") return ApiResponse<InvoiceDto>.Fail("Only DRAFT invoices can be sent.");
            entity.Status = "SENT";
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<InvoiceDto>.Ok(InvoiceMapper.Map(entity), "Invoice sent.");
        }
    }

    // ── CANCEL INVOICE ────────────────────────────────────────────────────────
    public class CancelInvoiceHandler : IRequestHandler<CancelInvoiceCommand, ApiResponse<InvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public CancelInvoiceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<InvoiceDto>> Handle(CancelInvoiceCommand request, CancellationToken ct)
        {
            var repo   = _uow.Repository<Invoice>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<InvoiceDto>.Fail("Invoice not found.");
            if (entity.Status == "PAID") return ApiResponse<InvoiceDto>.Fail("Cannot cancel a paid invoice.");
            entity.Status = "CANCELLED";
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<InvoiceDto>.Ok(InvoiceMapper.Map(entity), "Invoice cancelled.");
        }
    }

    // ── RECORD DIRECT PAYMENT (Cash / EcoCash / Bank Transfer) ───────────────
    // Creates a Payment record + Receipt + updates invoice balance
    public class RecordPaymentHandler : IRequestHandler<RecordPaymentCommand, ApiResponse<ReceiptDto>>
    {
        private readonly IUnitOfWork _uow;
        public RecordPaymentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ReceiptDto>> Handle(RecordPaymentCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var invoiceRepo = _uow.Repository<Invoice>();
            var invoice = await invoiceRepo.GetByIdAsync(d.InvoiceId, ct);
            if (invoice == null) return ApiResponse<ReceiptDto>.Fail("Invoice not found.");
            if (invoice.Status == "PAID") return ApiResponse<ReceiptDto>.Fail("Invoice is already fully paid.");
            if (invoice.Status == "CANCELLED") return ApiResponse<ReceiptDto>.Fail("Cannot pay a cancelled invoice.");
            if (d.Amount <= 0) return ApiResponse<ReceiptDto>.Fail("Payment amount must be greater than zero.");
            if (d.Amount > invoice.BalanceAmount) return ApiResponse<ReceiptDto>.Fail($"Amount exceeds balance of {invoice.BalanceAmount:F2}.");

            // Generate receipt number
            var receiptCount = await _uow.Repository<Receipt>().TableNoTracking.CountAsync(ct);
            var receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{(receiptCount + 1):D4}";

            // Update invoice
            invoice.PaidAmount    += d.Amount;
            invoice.BalanceAmount -= d.Amount;
            invoice.Status = invoice.BalanceAmount <= 0 ? "PAID" : "PARTIAL";
            await invoiceRepo.UpdateAsync(invoice, ct);

            // Create receipt (SourceId = 0 for direct payment, no suspense)
            var receipt = new Receipt
            {
                ReceiptNumber  = receiptNumber,
                InvoiceId      = d.InvoiceId,
                Amount         = d.Amount,
                Status         = "ACTIVE",
                CompanyId      = d.CompanyId,
                CurrencyId     = 1, // default currency
                SuspenseId     = 1, // placeholder — direct payment has no suspense
                ExchangeRateId = 1, // placeholder
                SourceId       = 0,
            };
            await _uow.Repository<Receipt>().AddAsync(receipt, ct);

            return ApiResponse<ReceiptDto>.Ok(new ReceiptDto
            {
                ReceiptNumber = receiptNumber,
                InvoiceId     = d.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                Amount        = d.Amount,
                Status        = "ACTIVE",
                CreatedAt     = DateTime.UtcNow,
            }, $"Payment of {d.Amount:F2} recorded. Receipt {receiptNumber} generated.");
        }
    }

    // ── SUBMIT BANK PAYMENT WITH PROOF OF PAYMENT ────────────────────────────
    // Customer submits bank payment + uploads POP → creates ProofofPayment + BankTransaction
    public class SubmitBankPaymentHandler : IRequestHandler<SubmitBankPaymentCommand, ApiResponse<ProofOfPaymentDto>>
    {
        private readonly IUnitOfWork _uow;
        public SubmitBankPaymentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ProofOfPaymentDto>> Handle(SubmitBankPaymentCommand request, CancellationToken ct)
        {
            var d = request.Dto;

            // Validate invoice exists
            var invoice = await _uow.Repository<Invoice>().GetByIdAsync(d.InvoiceId, ct);
            if (invoice == null) return ApiResponse<ProofOfPaymentDto>.Fail("Invoice not found.");
            if (invoice.Status == "PAID") return ApiResponse<ProofOfPaymentDto>.Fail("Invoice is already fully paid.");
            if (invoice.Status == "CANCELLED") return ApiResponse<ProofOfPaymentDto>.Fail("Cannot pay a cancelled invoice.");

            // Save POP file if provided (base64 → file path)
            string? filePath = null;
            if (!string.IsNullOrEmpty(d.PopFileBase64) && !string.IsNullOrEmpty(d.PopFileName))
            {
                try
                {
                    // Store as base64 string in the file field (or decode and save to disk)
                    // For simplicity we store the base64 data URI directly
                    // In production you'd save to disk/blob storage and store the path
                    filePath = $"data:application/octet-stream;base64,{d.PopFileBase64}";
                }
                catch
                {
                    filePath = d.PopFileName; // fallback to filename only
                }
            }

            // Create Proof of Payment record
            var pop = new ProofofPayment
            {
                InvoiceId = d.InvoiceId,
                file      = filePath ?? d.PopFileName,
                Status    = "PENDING",
                CompanyId = d.CompanyId,
            };
            await _uow.Repository<ProofofPayment>().AddAsync(pop, ct);

            // Create Bank Transaction linked to the POP
            var bankTx = new BankTransaction
            {
                Name               = $"Payment for {invoice.InvoiceNumber}",
                BankId             = d.BankId,
                CustomerId         = d.CustomerId,
                CurrencyId         = d.CurrencyId,
                ProofofPaymentId   = pop.Id,
                Amount             = d.Amount.ToString("F2"),
                AccountNumber      = d.AccountNumber,
                StatementReference = d.StatementReference,
                Description        = d.Description ?? $"Bank payment for invoice {invoice.InvoiceNumber}",
                TransactionDate    = d.TransactionDate,
                Status             = "PENDING",
                CompanyId          = d.CompanyId,
            };
            await _uow.Repository<BankTransaction>().AddAsync(bankTx, ct);

            return ApiResponse<ProofOfPaymentDto>.Ok(new ProofOfPaymentDto
            {
                Id                = pop.Id,
                InvoiceId         = pop.InvoiceId,
                InvoiceNumber     = invoice.InvoiceNumber,
                FilePath          = pop.file,
                Status            = pop.Status,
                CreatedAt         = pop.CreatedAt,
                BankTransactionId = bankTx.Id,
            }, $"Bank payment submitted. POP saved. Awaiting finance review.");
        }
    }

    // ── GET PROOF OF PAYMENTS ─────────────────────────────────────────────────
    public class GetProofOfPaymentsByCompanyHandler : IRequestHandler<GetProofOfPaymentsByCompanyQuery, ApiResponse<List<ProofOfPaymentDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetProofOfPaymentsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<ProofOfPaymentDto>>> Handle(GetProofOfPaymentsByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<ProofofPayment>()
                .TableNoTracking
                .Where(p => p.CompanyId == request.CompanyId)
                .Include(p => p.Invoice)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            return ApiResponse<List<ProofOfPaymentDto>>.Ok(list.Select(p => new ProofOfPaymentDto
            {
                Id            = p.Id,
                InvoiceId     = p.InvoiceId,
                InvoiceNumber = p.Invoice?.InvoiceNumber,
                FilePath      = p.file,
                Status        = p.Status,
                CreatedAt     = p.CreatedAt,
            }).ToList());
        }
    }

    public class GetProofOfPaymentsByInvoiceHandler : IRequestHandler<GetProofOfPaymentsByInvoiceQuery, ApiResponse<List<ProofOfPaymentDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetProofOfPaymentsByInvoiceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<ProofOfPaymentDto>>> Handle(GetProofOfPaymentsByInvoiceQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<ProofofPayment>()
                .TableNoTracking
                .Where(p => p.InvoiceId == request.InvoiceId)
                .Include(p => p.Invoice)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            return ApiResponse<List<ProofOfPaymentDto>>.Ok(list.Select(p => new ProofOfPaymentDto
            {
                Id            = p.Id,
                InvoiceId     = p.InvoiceId,
                InvoiceNumber = p.Invoice?.InvoiceNumber,
                FilePath      = p.file,
                Status        = p.Status,
                CreatedAt     = p.CreatedAt,
            }).ToList());
        }
    }
    public class ClaimBankTransactionHandler : IRequestHandler<ClaimBankTransactionCommand, ApiResponse<SuspenseDto>>
    {
        private readonly IUnitOfWork _uow;
        public ClaimBankTransactionHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<SuspenseDto>> Handle(ClaimBankTransactionCommand request, CancellationToken ct)
        {
            var d = request.Dto;

            // Get the bank transaction
            var txRepo = _uow.Repository<BankTransaction>();
            var tx = await txRepo.GetByIdAsync(d.BankTransactionId, ct);
            if (tx == null) return ApiResponse<SuspenseDto>.Fail("Bank transaction not found.");
            if (tx.Status == "CLAIMED") return ApiResponse<SuspenseDto>.Fail("This transaction has already been claimed.");

            // Parse amount
            if (!decimal.TryParse(tx.Amount, out var amount) || amount <= 0)
                return ApiResponse<SuspenseDto>.Fail("Invalid transaction amount.");

            // Mark bank transaction as CLAIMED
            tx.Status = "CLAIMED";
            await txRepo.UpdateAsync(tx, ct);

            // Also update the linked Proof of Payment status to CLAIMED
            if (tx.ProofofPaymentId > 0)
            {
                var popRepo = _uow.Repository<ProofofPayment>();
                var pop = await popRepo.GetByIdAsync(tx.ProofofPaymentId, ct);
                if (pop != null)
                {
                    pop.Status = "CLAIMED";
                    await popRepo.UpdateAsync(pop, ct);
                }
            }

            // Create suspense entry
            var suspense = new Suspense
            {
                CustomerId = d.CustomerId,
                CurrencyId = d.CurrencyId,
                SourceId   = d.BankTransactionId,
                Amount     = amount,
                Status     = "PENDING",
                CompanyId  = d.CompanyId,
            };
            await _uow.Repository<Suspense>().AddAsync(suspense, ct);

            return ApiResponse<SuspenseDto>.Ok(new SuspenseDto
            {
                Id         = suspense.Id,
                CustomerId = suspense.CustomerId,
                CurrencyId = suspense.CurrencyId,
                SourceId   = suspense.SourceId,
                Amount     = suspense.Amount,
                Status     = suspense.Status,
                CompanyId  = suspense.CompanyId,
                CreatedAt  = DateTime.UtcNow,
            }, $"Bank transaction claimed. POP marked CLAIMED. Suspense of {amount:F2} created.");
        }
    }

    // ── APPLY SUSPENSE TO INVOICE → GENERATE RECEIPT ─────────────────────────
    public class ApplySuspenseHandler : IRequestHandler<ApplySuspenseCommand, ApiResponse<ReceiptDto>>
    {
        private readonly IUnitOfWork _uow;
        public ApplySuspenseHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<ReceiptDto>> Handle(ApplySuspenseCommand request, CancellationToken ct)
        {
            var d = request.Dto;

            // Get suspense
            var suspenseRepo = _uow.Repository<Suspense>();
            var suspense = await suspenseRepo.GetByIdAsync(d.SuspenseId, ct);
            if (suspense == null) return ApiResponse<ReceiptDto>.Fail("Suspense entry not found.");
            if (suspense.Status == "UTILIZED") return ApiResponse<ReceiptDto>.Fail("This suspense has already been utilized.");
            if (d.Amount > suspense.Amount) return ApiResponse<ReceiptDto>.Fail($"Amount exceeds suspense balance of {suspense.Amount:F2}.");

            // Get invoice
            var invoiceRepo = _uow.Repository<Invoice>();
            var invoice = await invoiceRepo.GetByIdAsync(d.InvoiceId, ct);
            if (invoice == null) return ApiResponse<ReceiptDto>.Fail("Invoice not found.");
            if (invoice.Status == "PAID") return ApiResponse<ReceiptDto>.Fail("Invoice is already fully paid.");
            if (invoice.Status == "CANCELLED") return ApiResponse<ReceiptDto>.Fail("Cannot pay a cancelled invoice.");
            if (d.Amount > invoice.BalanceAmount) return ApiResponse<ReceiptDto>.Fail($"Amount exceeds invoice balance of {invoice.BalanceAmount:F2}.");

            // Generate receipt number
            var receiptCount = await _uow.Repository<Receipt>().TableNoTracking.CountAsync(ct);
            var receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{(receiptCount + 1):D4}";

            // Update invoice
            invoice.PaidAmount    += d.Amount;
            invoice.BalanceAmount -= d.Amount;
            invoice.Status = invoice.BalanceAmount <= 0 ? "PAID" : "PARTIAL";
            await invoiceRepo.UpdateAsync(invoice, ct);

            // Mark suspense as UTILIZED (or reduce if partial)
            suspense.Status = "UTILIZED";
            await suspenseRepo.UpdateAsync(suspense, ct);

            // Create receipt
            var receipt = new Receipt
            {
                ReceiptNumber  = receiptNumber,
                InvoiceId      = d.InvoiceId,
                CustomerId     = suspense.CustomerId,
                Amount         = d.Amount,
                Status         = "ACTIVE",
                CompanyId      = d.CompanyId,
                CurrencyId     = suspense.CurrencyId,
                SuspenseId     = d.SuspenseId,
                ExchangeRateId = d.ExchangeRateId > 0 ? d.ExchangeRateId : 1,
                SourceId       = suspense.SourceId,
            };
            await _uow.Repository<Receipt>().AddAsync(receipt, ct);

            return ApiResponse<ReceiptDto>.Ok(new ReceiptDto
            {
                ReceiptNumber = receiptNumber,
                InvoiceId     = d.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                Amount        = d.Amount,
                Status        = "ACTIVE",
                CreatedAt     = DateTime.UtcNow,
            }, $"Suspense applied. Receipt {receiptNumber} generated. Invoice status: {invoice.Status}.");
        }
    }

    // ── GET SUSPENSE ──────────────────────────────────────────────────────────
    public class GetSuspenseByCompanyHandler : IRequestHandler<GetSuspenseByCompanyQuery, ApiResponse<List<SuspenseDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetSuspenseByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<SuspenseDto>>> Handle(GetSuspenseByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Suspense>()
                .TableNoTracking
                .Where(s => s.CompanyId == request.CompanyId)
                .Include(s => s.Customer)
                .Include(s => s.Currency)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(ct);

            return ApiResponse<List<SuspenseDto>>.Ok(list.Select(s => new SuspenseDto
            {
                Id           = s.Id,
                CustomerId   = s.CustomerId,
                CustomerName = s.Customer?.FullName,
                CurrencyId   = s.CurrencyId,
                CurrencyCode = s.Currency?.Code,
                SourceId     = s.SourceId,
                Amount       = s.Amount,
                Status       = s.Status,
                CompanyId    = s.CompanyId,
                CreatedAt    = s.CreatedAt,
            }).ToList());
        }
    }

    // ── GET RECEIPTS ──────────────────────────────────────────────────────────
    public class GetReceiptsByCompanyHandler : IRequestHandler<GetReceiptsByCompanyQuery, ApiResponse<List<ReceiptDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetReceiptsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<ReceiptDto>>> Handle(GetReceiptsByCompanyQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Receipt>()
                .TableNoTracking
                .Where(r => r.CompanyId == request.CompanyId)
                .Include(r => r.Customer)
                .Include(r => r.Invoice)
                .Include(r => r.Currency)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);

            return ApiResponse<List<ReceiptDto>>.Ok(list.Select(r => new ReceiptDto
            {
                Id            = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                CustomerId    = r.CustomerId,
                CustomerName  = r.Customer?.FullName,
                InvoiceId     = r.InvoiceId,
                InvoiceNumber = r.Invoice?.InvoiceNumber,
                Amount        = r.Amount,
                Status        = r.Status,
                CurrencyCode  = r.Currency?.Code,
                CreatedAt     = r.CreatedAt,
            }).ToList());
        }
    }

    public class GetReceiptsByInvoiceHandler : IRequestHandler<GetReceiptsByInvoiceQuery, ApiResponse<List<ReceiptDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetReceiptsByInvoiceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<ReceiptDto>>> Handle(GetReceiptsByInvoiceQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<Receipt>()
                .TableNoTracking
                .Where(r => r.InvoiceId == request.InvoiceId)
                .Include(r => r.Customer)
                .Include(r => r.Currency)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);

            return ApiResponse<List<ReceiptDto>>.Ok(list.Select(r => new ReceiptDto
            {
                Id            = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                CustomerId    = r.CustomerId,
                CustomerName  = r.Customer?.FullName,
                InvoiceId     = r.InvoiceId,
                Amount        = r.Amount,
                Status        = r.Status,
                CurrencyCode  = r.Currency?.Code,
                CreatedAt     = r.CreatedAt,
            }).ToList());
        }
    }

    // ── MAPPER ────────────────────────────────────────────────────────────────
    internal static class InvoiceMapper
    {
        public static InvoiceDto Map(Invoice i) => new()
        {
            Id              = i.Id,
            InvoiceNumber   = i.InvoiceNumber,
            CompanyId       = i.CompanyId,
            CustomerId      = i.CustomerId,
            CustomerName    = i.CustomerName,
            CustomerEmail   = i.CustomerEmail,
            CustomerPhone   = i.CustomerPhone,
            InvoiceDate     = i.InvoiceDate,
            DueDate         = i.DueDate,
            SubTotal        = i.SubTotal,
            TaxAmount       = i.TaxAmount,
            DiscountAmount  = i.DiscountAmount,
            TotalAmount     = i.TotalAmount,
            PaidAmount      = i.PaidAmount,
            BalanceAmount   = i.BalanceAmount,
            Status          = i.Status,
            Notes           = i.Notes,
            Terms           = i.Terms,
            CreatedAt       = i.CreatedAt,
            CustomerCompanyId = i.CustomerCompanyId, BillToName = i.BillToName, BillToAddress = i.BillToAddress, BillToTaxNumber = i.BillToTaxNumber,
            BillToVatNumber = i.BillToVatNumber, BillToEmail = i.BillToEmail, BillToPhone = i.BillToPhone, ContactPerson = i.ContactPerson,
            PurchaseOrderNumber = i.PurchaseOrderNumber, CurrencyCode = i.CurrencyCode, IsFiscal = i.IsFiscal, FiscalInvoiceNumber = i.FiscalInvoiceNumber,
            FiscalDeviceSerial = i.FiscalDeviceSerial, FiscalVerificationCode = i.FiscalVerificationCode, FiscalDate = i.FiscalDate,
            EmailedAt = i.EmailedAt, EmailedTo = i.EmailedTo,
            PendingAmount = i.Ledger?.Where(p => p.Status == "PENDING").Sum(p => p.Amount) ?? 0,
            Payments = i.Ledger?.OrderBy(p => p.CreatedAt).Select(p => Billing.InvoicePaymentSupport.Map(p, i)).ToList() ?? new(),
            Items           = i.Items?.Select(item => new InvoiceItemDto
            {
                Id           = item.Id,
                Description  = item.Description,
                Quantity     = item.Quantity,
                UnitPrice    = item.UnitPrice,
                TaxRate      = item.TaxRate,
                DiscountRate = item.DiscountRate,
                Amount       = item.Amount,
            }).ToList() ?? new(),
        };
    }
}
