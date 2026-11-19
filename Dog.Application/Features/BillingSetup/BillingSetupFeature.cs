using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.BillingSetup
{
    // ── Company billing profile: the legal / tax details printed on every invoice ──
    public class BillingProfileDto
    {
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? TaxNumber { get; set; }
        public string? VatNumber { get; set; }
        public string? InvoiceTerms { get; set; }
        public string? InvoiceFooter { get; set; }
        public decimal DefaultVatRate { get; set; }
        public bool FiscalEnabled { get; set; }
        public string? FiscalDeviceSerial { get; set; }
    }

    public record GetBillingProfileQuery(int CompanyId) : IRequest<ApiResponse<BillingProfileDto>>;
    public record SaveBillingProfileCommand(BillingProfileDto Dto) : IRequest<ApiResponse<BillingProfileDto>>;

    // ── Email (SMTP) settings ──
    public class EmailSettingDto
    {
        public int CompanyId { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Security { get; set; } = "StartTls";
        public string? Username { get; set; }
        /// <summary>Only sent when changing the password; never returned. Leave empty to keep the saved one.</summary>
        public string? Password { get; set; }
        public bool HasPassword { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string? FromName { get; set; }
        public string? ReplyTo { get; set; }
        public string? BccAddress { get; set; }
        public string? Signature { get; set; }
        public bool Configured { get; set; }
        public DateTime? LastTestAt { get; set; }
        public bool? LastTestOk { get; set; }
        public string? LastTestMessage { get; set; }
    }

    public class EmailLogDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ToAddresses { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? RelatedType { get; set; }
        public int? RelatedId { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? SentByName { get; set; }
        public bool HasAttachment { get; set; }
    }

    public record GetEmailSettingQuery(int CompanyId) : IRequest<ApiResponse<EmailSettingDto>>;
    public record SaveEmailSettingCommand(EmailSettingDto Dto) : IRequest<ApiResponse<EmailSettingDto>>;
    public record TestEmailSettingCommand(int CompanyId, string ToAddress, string? SentByName) : IRequest<ApiResponse<EmailSettingDto>>;
    public record GetEmailLogsQuery(int CompanyId, string? RelatedType, int? RelatedId) : IRequest<ApiResponse<List<EmailLogDto>>>;

    // ── Saved items for the invoice builder ──
    public class CatalogItemDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
    }

    public record GetCatalogQuery(int CompanyId) : IRequest<ApiResponse<List<CatalogItemDto>>>;
    public record SaveCatalogItemCommand(CatalogItemDto Dto) : IRequest<ApiResponse<CatalogItemDto>>;
    public record DeleteCatalogItemCommand(int Id) : IRequest<ApiResponse<bool>>;

    /// <summary>Remembers who an invoice was emailed to (company or training invoice).</summary>
    public record MarkInvoiceEmailedCommand(bool Training, int InvoiceId, string To) : IRequest<ApiResponse<bool>>;

    public class BillingProfileHandlers :
        IRequestHandler<GetBillingProfileQuery, ApiResponse<BillingProfileDto>>,
        IRequestHandler<SaveBillingProfileCommand, ApiResponse<BillingProfileDto>>
    {
        private readonly IUnitOfWork _uow;
        public BillingProfileHandlers(IUnitOfWork uow) => _uow = uow;

        private static BillingProfileDto Map(Company c) => new()
        {
            CompanyId = c.Id, CompanyName = c.CompanyName, RegistrationNumber = c.RegistrationNumber, TaxNumber = c.TaxNumber, VatNumber = c.VatNumber,
            InvoiceTerms = c.InvoiceTerms, InvoiceFooter = c.InvoiceFooter, DefaultVatRate = c.DefaultVatRate,
            FiscalEnabled = c.FiscalEnabled, FiscalDeviceSerial = c.FiscalDeviceSerial,
        };

        public async Task<ApiResponse<BillingProfileDto>> Handle(GetBillingProfileQuery request, CancellationToken ct)
        {
            var c = await _uow.Repository<Company>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.CompanyId, ct);
            return c == null ? ApiResponse<BillingProfileDto>.Fail("Company not found.") : ApiResponse<BillingProfileDto>.Ok(Map(c));
        }

        public async Task<ApiResponse<BillingProfileDto>> Handle(SaveBillingProfileCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (d.DefaultVatRate is < 0 or > 100) return ApiResponse<BillingProfileDto>.Fail("The VAT rate must be between 0 and 100.");
            if (d.FiscalEnabled && string.IsNullOrWhiteSpace(d.FiscalDeviceSerial))
                return ApiResponse<BillingProfileDto>.Fail("Enter the fiscal device serial number to turn fiscal invoices on.");
            var c = await _uow.Repository<Company>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.CompanyId, ct);
            if (c == null) return ApiResponse<BillingProfileDto>.Fail("Company not found.");
            static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            c.RegistrationNumber = T(d.RegistrationNumber); c.TaxNumber = T(d.TaxNumber); c.VatNumber = T(d.VatNumber);
            c.InvoiceTerms = T(d.InvoiceTerms); c.InvoiceFooter = T(d.InvoiceFooter); c.DefaultVatRate = d.DefaultVatRate;
            c.FiscalEnabled = d.FiscalEnabled; c.FiscalDeviceSerial = T(d.FiscalDeviceSerial);
            await _uow.Repository<Company>().UpdateAsync(c, ct);
            return ApiResponse<BillingProfileDto>.Ok(Map(c), "Billing details saved.");
        }
    }

    public class EmailSettingHandlers :
        IRequestHandler<GetEmailSettingQuery, ApiResponse<EmailSettingDto>>,
        IRequestHandler<SaveEmailSettingCommand, ApiResponse<EmailSettingDto>>,
        IRequestHandler<TestEmailSettingCommand, ApiResponse<EmailSettingDto>>,
        IRequestHandler<GetEmailLogsQuery, ApiResponse<List<EmailLogDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailService _email;
        public EmailSettingHandlers(IUnitOfWork uow, IEmailService email) { _uow = uow; _email = email; }

        private static EmailSettingDto Map(int companyId, EmailSetting? e) => e == null
            ? new EmailSettingDto { CompanyId = companyId }
            : new EmailSettingDto
            {
                CompanyId = companyId, Host = e.Host, Port = e.Port, Security = e.Security, Username = e.Username, HasPassword = !string.IsNullOrEmpty(e.PasswordEncrypted),
                FromAddress = e.FromAddress, FromName = e.FromName, ReplyTo = e.ReplyTo, BccAddress = e.BccAddress, Signature = e.Signature, Configured = true,
                LastTestAt = e.LastTestAt, LastTestOk = e.LastTestOk, LastTestMessage = e.LastTestMessage,
            };

        public async Task<ApiResponse<EmailSettingDto>> Handle(GetEmailSettingQuery request, CancellationToken ct)
        {
            var e = await _uow.Repository<EmailSetting>().TableNoTracking.FirstOrDefaultAsync(x => x.CompanyId == request.CompanyId, ct);
            return ApiResponse<EmailSettingDto>.Ok(Map(request.CompanyId, e));
        }

        public async Task<ApiResponse<EmailSettingDto>> Handle(SaveEmailSettingCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (string.IsNullOrWhiteSpace(d.Host)) return ApiResponse<EmailSettingDto>.Fail("Enter the SMTP server (e.g. mail.yourdomain.co.zw).");
            if (d.Port is < 1 or > 65535) return ApiResponse<EmailSettingDto>.Fail("The port must be between 1 and 65535.");
            if (string.IsNullOrWhiteSpace(d.FromAddress) || !d.FromAddress.Contains('@')) return ApiResponse<EmailSettingDto>.Fail("Enter the 'from' email address, e.g. billing@yourdomain.co.zw.");
            var sec = d.Security is "None" or "SslOnConnect" or "StartTls" ? d.Security : "StartTls";

            var repo = _uow.Repository<EmailSetting>();
            var e = await repo.Table.AsTracking().FirstOrDefaultAsync(x => x.CompanyId == d.CompanyId, ct);
            var isNew = e == null;
            e ??= new EmailSetting { CompanyId = d.CompanyId };
            static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            e.Host = d.Host.Trim(); e.Port = d.Port; e.Security = sec; e.Username = T(d.Username);
            if (!string.IsNullOrEmpty(d.Password)) e.PasswordEncrypted = _email.Encrypt(d.Password);
            e.FromAddress = d.FromAddress.Trim(); e.FromName = T(d.FromName); e.ReplyTo = T(d.ReplyTo); e.BccAddress = T(d.BccAddress); e.Signature = T(d.Signature);
            e.LastTestAt = null; e.LastTestOk = null; e.LastTestMessage = null;
            if (isNew) await repo.AddAsync(e, ct); else await repo.UpdateAsync(e, ct);
            return ApiResponse<EmailSettingDto>.Ok(Map(d.CompanyId, e), "Email settings saved. Send a test email to confirm they work.");
        }

        public async Task<ApiResponse<EmailSettingDto>> Handle(TestEmailSettingCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.ToAddress) || !request.ToAddress.Contains('@'))
                return ApiResponse<EmailSettingDto>.Fail("Enter the address to send the test to.");
            var res = await _email.TestAsync(request.CompanyId, request.ToAddress.Trim(), request.SentByName, ct);
            var repo = _uow.Repository<EmailSetting>();
            var e = await repo.Table.AsTracking().FirstOrDefaultAsync(x => x.CompanyId == request.CompanyId, ct);
            if (e != null)
            {
                e.LastTestAt = DateTime.UtcNow; e.LastTestOk = res.Success; e.LastTestMessage = res.Success ? "Test email sent." : res.Error;
                await repo.UpdateAsync(e, ct);
            }
            return res.Success
                ? ApiResponse<EmailSettingDto>.Ok(Map(request.CompanyId, e), $"Test email sent to {request.ToAddress}.")
                : ApiResponse<EmailSettingDto>.Fail(res.Error ?? "The test email could not be sent.");
        }

        public async Task<ApiResponse<List<EmailLogDto>>> Handle(GetEmailLogsQuery request, CancellationToken ct)
        {
            var q = _uow.Repository<EmailLog>().TableNoTracking.Where(x => x.CompanyId == request.CompanyId);
            if (!string.IsNullOrWhiteSpace(request.RelatedType)) q = q.Where(x => x.RelatedType == request.RelatedType);
            if (request.RelatedId is > 0) q = q.Where(x => x.RelatedId == request.RelatedId);
            var list = await q.OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(ct);
            return ApiResponse<List<EmailLogDto>>.Ok(list.Select(x => new EmailLogDto
            {
                Id = x.Id, CreatedAt = x.CreatedAt, ToAddresses = x.ToAddresses, Subject = x.Subject, RelatedType = x.RelatedType, RelatedId = x.RelatedId,
                Success = x.Success, Error = x.Error, SentByName = x.SentByName, HasAttachment = x.HasAttachment
            }).ToList());
        }
    }

    public class CatalogHandlers :
        IRequestHandler<GetCatalogQuery, ApiResponse<List<CatalogItemDto>>>,
        IRequestHandler<SaveCatalogItemCommand, ApiResponse<CatalogItemDto>>,
        IRequestHandler<DeleteCatalogItemCommand, ApiResponse<bool>>,
        IRequestHandler<MarkInvoiceEmailedCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public CatalogHandlers(IUnitOfWork uow) => _uow = uow;

        private static CatalogItemDto Map(InvoiceCatalogItem i) => new() { Id = i.Id, CompanyId = i.CompanyId, Name = i.Name, Description = i.Description, UnitPrice = i.UnitPrice, TaxRate = i.TaxRate };

        public async Task<ApiResponse<List<CatalogItemDto>>> Handle(GetCatalogQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<InvoiceCatalogItem>().TableNoTracking.Where(x => x.CompanyId == request.CompanyId).OrderBy(x => x.Name).ToListAsync(ct);
            return ApiResponse<List<CatalogItemDto>>.Ok(list.Select(Map).ToList());
        }

        public async Task<ApiResponse<CatalogItemDto>> Handle(SaveCatalogItemCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (string.IsNullOrWhiteSpace(d.Name)) return ApiResponse<CatalogItemDto>.Fail("Enter the item name.");
            if (d.UnitPrice < 0) return ApiResponse<CatalogItemDto>.Fail("The price cannot be negative.");
            if (d.TaxRate is < 0 or > 100) return ApiResponse<CatalogItemDto>.Fail("The tax rate must be between 0 and 100.");
            var repo = _uow.Repository<InvoiceCatalogItem>();
            InvoiceCatalogItem? e = d.Id > 0 ? await repo.Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.Id, ct) : null;
            var isNew = e == null;
            e ??= new InvoiceCatalogItem { CompanyId = d.CompanyId };
            e.Name = d.Name.Trim(); e.Description = string.IsNullOrWhiteSpace(d.Description) ? null : d.Description.Trim(); e.UnitPrice = d.UnitPrice; e.TaxRate = d.TaxRate;
            if (isNew) await repo.AddAsync(e, ct); else await repo.UpdateAsync(e, ct);
            return ApiResponse<CatalogItemDto>.Ok(Map(e), "Item saved.");
        }

        public async Task<ApiResponse<bool>> Handle(DeleteCatalogItemCommand request, CancellationToken ct)
        {
            var e = await _uow.Repository<InvoiceCatalogItem>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (e == null) return ApiResponse<bool>.Fail("Item not found.");
            await _uow.Repository<InvoiceCatalogItem>().DeleteAsync(e, ct);
            return ApiResponse<bool>.Ok(true, "Item deleted.");
        }

        public async Task<ApiResponse<bool>> Handle(MarkInvoiceEmailedCommand request, CancellationToken ct)
        {
            if (request.Training)
            {
                var t = await _uow.Repository<DogTrainingInvoice>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.InvoiceId, ct);
                if (t == null) return ApiResponse<bool>.Fail("Invoice not found.");
                t.EmailedAt = DateTime.UtcNow; t.EmailedTo = request.To;
                if (t.Status == "DRAFT") t.Status = "SENT";
                await _uow.Repository<DogTrainingInvoice>().UpdateAsync(t, ct);
            }
            else
            {
                var i = await _uow.Repository<Invoice>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.InvoiceId, ct);
                if (i == null) return ApiResponse<bool>.Fail("Invoice not found.");
                i.EmailedAt = DateTime.UtcNow; i.EmailedTo = request.To;
                if (i.Status == "DRAFT") i.Status = "SENT";
                await _uow.Repository<Invoice>().UpdateAsync(i, ct);
            }
            return ApiResponse<bool>.Ok(true);
        }
    }
}

namespace Dog.Application.Features.BillingSetup
{
    public class PdfInfoDto
    {
        public BillingProfileDto? Profile { get; set; }
        public List<string> Banks { get; set; } = new();
        /// <summary>Our own bank accounts a customer can pay into (id + display label) - for the payment form.</summary>
        public List<BankAccountOptionDto> BankAccounts { get; set; } = new();
    }

    public class BankAccountOptionDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public record GetPdfInfoQuery(int CompanyId) : IRequest<ApiResponse<PdfInfoDto>>;

    /// <summary>What a printed invoice needs besides the invoice itself: the company's tax profile and bank accounts.</summary>
    public class GetPdfInfoHandler : IRequestHandler<GetPdfInfoQuery, ApiResponse<PdfInfoDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetPdfInfoHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<PdfInfoDto>> Handle(GetPdfInfoQuery request, CancellationToken ct)
        {
            var c = await _uow.Repository<Company>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.CompanyId, ct);
            var bankOwner = request.CompanyId;
            // A branch that has not filled in its own legal details / bank accounts prints its head office's.
            if (c?.ParentCompanyId != null && string.IsNullOrWhiteSpace(c.RegistrationNumber) && string.IsNullOrWhiteSpace(c.TaxNumber) && string.IsNullOrWhiteSpace(c.VatNumber))
            {
                var parent = await _uow.Repository<Company>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == c.ParentCompanyId, ct);
                if (parent != null) { c.RegistrationNumber = parent.RegistrationNumber; c.TaxNumber = parent.TaxNumber; c.VatNumber = parent.VatNumber; c.InvoiceTerms ??= parent.InvoiceTerms; c.InvoiceFooter ??= parent.InvoiceFooter; }
            }
            var accounts = await _uow.Repository<BankAccount>().TableNoTracking.Include(b => b.Bank).Where(b => b.CompanyId == bankOwner && b.IsActive).OrderBy(b => b.Id).Take(6).ToListAsync(ct);
            if (accounts.Count == 0 && c?.ParentCompanyId != null)
                accounts = await _uow.Repository<BankAccount>().TableNoTracking.Include(b => b.Bank).Where(b => b.CompanyId == c.ParentCompanyId && b.IsActive).OrderBy(b => b.Id).Take(6).ToListAsync(ct);
            return ApiResponse<PdfInfoDto>.Ok(new PdfInfoDto
            {
                Profile = c == null ? null : new BillingProfileDto
                {
                    CompanyId = c.Id, CompanyName = c.CompanyName, RegistrationNumber = c.RegistrationNumber, TaxNumber = c.TaxNumber, VatNumber = c.VatNumber,
                    InvoiceTerms = c.InvoiceTerms, InvoiceFooter = c.InvoiceFooter, DefaultVatRate = c.DefaultVatRate, FiscalEnabled = c.FiscalEnabled, FiscalDeviceSerial = c.FiscalDeviceSerial,
                },
                BankAccounts = accounts.Select(b => new BankAccountOptionDto { Id = b.Id, Label = string.Join(" · ", new[] { b.Name, b.Bank?.Name, b.AccountNumber }.Where(x => !string.IsNullOrWhiteSpace(x))) }).ToList(),
                Banks = accounts.Select(b => string.Join("  ·  ", new[]
                {
                    b.Bank?.Name, b.Name, string.IsNullOrWhiteSpace(b.AccountNumber) ? null : $"Account {b.AccountNumber}",
                    string.IsNullOrWhiteSpace(b.BranchName) ? null : $"Branch {b.BranchName}", string.IsNullOrWhiteSpace(b.SwiftCode) ? null : $"SWIFT {b.SwiftCode}"
                }.Where(s => !string.IsNullOrWhiteSpace(s)))).ToList(),
            });
        }
    }
}
