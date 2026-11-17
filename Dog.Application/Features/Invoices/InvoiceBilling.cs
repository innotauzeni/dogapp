using Dog.Application.Common;
using Dog.Application.Common.Payments;
using Dog.Application.Contracts;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Invoices.Billing
{
    /// <summary>The signed-in user doing a payment action. CanVerify = may confirm bank / cheque payments.</summary>
    public record PaymentActor(string UserId, string Name, int CompanyId, bool CanVerify);

    public class InvoicePaymentDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? BillTo { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public DateTime? ValueDate { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public string Method { get; set; } = string.Empty;
        public string? MethodLabel { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PayerName { get; set; }
        public string? PayerContact { get; set; }
        public string? PayerBank { get; set; }
        public string? PayerAccount { get; set; }
        public int? DestinationBankAccountId { get; set; }
        public string? DestinationAccountLabel { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ChequeNumber { get; set; }
        public string? ProofUrl { get; set; }
        public string? Notes { get; set; }
        public string? ReceivedByName { get; set; }
        public string? ConfirmedByName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? StatementReference { get; set; }
        public string? RejectReason { get; set; }
    }

    public class RecordInvoicePaymentDto : PaymentInput
    {
        public int InvoiceId { get; set; }
    }

    public class ReviewPaymentDto
    {
        public string? StatementReference { get; set; }
        public string? Reason { get; set; }
    }

    public record RecordInvoicePaymentCommand(PaymentActor Actor, RecordInvoicePaymentDto Dto) : IRequest<ApiResponse<InvoicePaymentDto>>;
    public record ConfirmInvoicePaymentCommand(PaymentActor Actor, int PaymentId, string? StatementReference) : IRequest<ApiResponse<InvoicePaymentDto>>;
    public record RejectInvoicePaymentCommand(PaymentActor Actor, int PaymentId, string? Reason) : IRequest<ApiResponse<InvoicePaymentDto>>;
    public record GetInvoicePaymentsQuery(int CompanyId, int? InvoiceId, string? Status) : IRequest<ApiResponse<List<InvoicePaymentDto>>>;
    public record GetPaymentMethodsQuery() : IRequest<ApiResponse<List<PaymentMethodInfo>>>;

    public static class InvoicePaymentSupport
    {
        public static async Task<Dictionary<int, string>> BankAccountLabelsAsync(IUnitOfWork uow, IEnumerable<int> ids, CancellationToken ct)
        {
            var list = ids.Distinct().ToList();
            if (list.Count == 0) return new();
            var rows = await uow.Repository<BankAccount>().TableNoTracking.Include(b => b.Bank).Where(b => list.Contains(b.Id)).ToListAsync(ct);
            return rows.ToDictionary(b => b.Id, b => string.Join(" · ", new[] { b.Name, b.Bank?.Name, b.AccountNumber }.Where(s => !string.IsNullOrWhiteSpace(s))));
        }

        public static InvoicePaymentDto Map(InvoicePayment p, Invoice? inv = null, IReadOnlyDictionary<int, string>? banks = null) => new()
        {
            Id = p.Id, InvoiceId = p.InvoiceId, InvoiceNumber = inv?.InvoiceNumber ?? p.Invoice?.InvoiceNumber,
            BillTo = inv?.BillToName ?? inv?.CustomerName ?? p.Invoice?.BillToName ?? p.Invoice?.CustomerName,
            PaymentNumber = p.PaymentNumber, PaymentDate = p.PaymentDate, ValueDate = p.ValueDate, Amount = p.Amount, CurrencyCode = p.CurrencyCode,
            Method = p.Method, MethodLabel = PaymentRules.Find(p.Method)?.Label ?? p.Method, Status = p.Status,
            PayerName = p.PayerName, PayerContact = p.PayerContact, PayerBank = p.PayerBank, PayerAccount = p.PayerAccount,
            DestinationBankAccountId = p.DestinationBankAccountId,
            DestinationAccountLabel = p.DestinationBankAccountId is int b && banks != null && banks.TryGetValue(b, out var l) ? l : null,
            ReferenceNumber = p.ReferenceNumber, ChequeNumber = p.ChequeNumber, ProofUrl = p.ProofUrl, Notes = p.Notes,
            ReceivedByName = p.ReceivedByName, ConfirmedByName = p.ConfirmedByName, ConfirmedAt = p.ConfirmedAt,
            StatementReference = p.StatementReference, RejectReason = p.RejectReason,
        };

        public static async Task<string> NextNumberAsync(IUnitOfWork uow, CancellationToken ct)
        {
            var prefix = $"RCP-{DateTime.UtcNow:yyyy}-";
            var count = await uow.Repository<InvoicePayment>().TableNoTracking.IgnoreQueryFilters().CountAsync(x => x.PaymentNumber.StartsWith(prefix), ct);
            string n;
            do { count++; n = $"{prefix}{count:D4}"; }
            while (await uow.Repository<InvoicePayment>().TableNoTracking.IgnoreQueryFilters().AnyAsync(x => x.PaymentNumber == n, ct));
            return n;
        }

        /// <summary>Applies a CONFIRMED payment to the invoice balance and status.</summary>
        public static void Apply(Invoice inv, decimal amount)
        {
            inv.PaidAmount += amount;
            inv.BalanceAmount = Math.Max(0, inv.TotalAmount - inv.PaidAmount);
            inv.Status = inv.BalanceAmount <= 0 ? "PAID" : "PARTIAL";
        }
    }

    public class GetPaymentMethodsHandler : IRequestHandler<GetPaymentMethodsQuery, ApiResponse<List<PaymentMethodInfo>>>
    {
        public Task<ApiResponse<List<PaymentMethodInfo>>> Handle(GetPaymentMethodsQuery request, CancellationToken ct)
            => Task.FromResult(ApiResponse<List<PaymentMethodInfo>>.Ok(PaymentRules.Methods.ToList()));
    }

    public class RecordInvoicePaymentHandler : IRequestHandler<RecordInvoicePaymentCommand, ApiResponse<InvoicePaymentDto>>
    {
        private readonly IUnitOfWork _uow;
        public RecordInvoicePaymentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<InvoicePaymentDto>> Handle(RecordInvoicePaymentCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (!request.Actor.CanVerify) d.VerifiedOnStatement = false;
            var err = PaymentRules.Validate(d);
            if (err != null) return ApiResponse<InvoicePaymentDto>.Fail(err);
            var info = PaymentRules.Find(d.Method)!;

            var invRepo = _uow.Repository<Invoice>();
            var inv = await invRepo.Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.InvoiceId, ct);
            if (inv == null) return ApiResponse<InvoicePaymentDto>.Fail("Invoice not found.");
            if (inv.Status == "CANCELLED") return ApiResponse<InvoicePaymentDto>.Fail("This invoice was cancelled.");
            if (inv.Status == "PAID" || inv.BalanceAmount <= 0) return ApiResponse<InvoicePaymentDto>.Fail("This invoice is already paid in full.");

            var pending = (await _uow.Repository<InvoicePayment>().TableNoTracking
                .Where(x => x.InvoiceId == inv.Id && x.Status == PaymentRules.Pending).Select(x => x.Amount).ToListAsync(ct)).Sum();
            var open = inv.BalanceAmount - pending;
            if (d.Amount > open)
                return ApiResponse<InvoicePaymentDto>.Fail(pending > 0
                    ? $"The amount is more than what is still open ({open:0.00}); {pending:0.00} is already waiting to be confirmed."
                    : $"The amount is more than the balance due ({inv.BalanceAmount:0.00}).");

            if (d.DestinationBankAccountId is > 0 &&
                !await _uow.Repository<BankAccount>().TableNoTracking.AnyAsync(b => b.Id == d.DestinationBankAccountId, ct))
                return ApiResponse<InvoicePaymentDto>.Fail("The selected receiving bank account does not exist.");

            // The same bank / mobile reference must never be used twice - this is what stops one transfer paying two invoices.
            var reference = PaymentRules.Trim(d.ReferenceNumber);
            if (reference.Length > 0)
            {
                var low = reference.ToLower();
                var used = await _uow.Repository<InvoicePayment>().TableNoTracking
                    .Where(x => x.Status != PaymentRules.Rejected && x.ReferenceNumber != null && x.ReferenceNumber.ToLower() == low && x.Method != "CASH")
                    .Select(x => new { x.PaymentNumber, x.InvoiceId }).FirstOrDefaultAsync(ct);
                if (used != null) return ApiResponse<InvoicePaymentDto>.Fail($"Reference '{reference}' was already used on payment {used.PaymentNumber}. Every transfer can be recorded only once.");
            }

            var status = PaymentRules.InitialStatus(info, request.Actor.CanVerify, d.VerifiedOnStatement);
            var p = new InvoicePayment
            {
                CompanyId = inv.CompanyId, InvoiceId = inv.Id, PaymentNumber = await InvoicePaymentSupport.NextNumberAsync(_uow, ct),
                PaymentDate = DateTime.UtcNow, ValueDate = d.ValueDate ?? d.PaymentDate, Amount = d.Amount,
                CurrencyCode = string.IsNullOrWhiteSpace(d.CurrencyCode) ? inv.CurrencyCode : d.CurrencyCode.Trim().ToUpperInvariant(),
                Method = info.Code, Status = status,
                PayerName = PaymentRules.Trim(d.PayerName).NullIfEmpty(), PayerContact = PaymentRules.Trim(d.PayerContact).NullIfEmpty(),
                PayerBank = PaymentRules.Trim(d.PayerBank).NullIfEmpty(), PayerAccount = PaymentRules.Trim(d.PayerAccount).NullIfEmpty(),
                DestinationBankAccountId = d.DestinationBankAccountId is > 0 ? d.DestinationBankAccountId : null,
                ReferenceNumber = reference.NullIfEmpty(), ChequeNumber = PaymentRules.Trim(d.ChequeNumber).NullIfEmpty(),
                ProofUrl = PaymentRules.Trim(d.ProofUrl).NullIfEmpty(), Notes = PaymentRules.Trim(d.Notes).NullIfEmpty(),
                ReceivedByUserId = request.Actor.UserId, ReceivedByName = request.Actor.Name,
            };
            if (status == PaymentRules.Confirmed)
            {
                p.ConfirmedByUserId = request.Actor.UserId; p.ConfirmedByName = request.Actor.Name; p.ConfirmedAt = DateTime.UtcNow;
                p.StatementReference = PaymentRules.Trim(d.StatementReference).NullIfEmpty();
            }
            await _uow.Repository<InvoicePayment>().AddAsync(p, ct);

            if (status == PaymentRules.Confirmed)
            {
                InvoicePaymentSupport.Apply(inv, p.Amount);
                await invRepo.UpdateAsync(inv, ct);
            }
            var banks = await InvoicePaymentSupport.BankAccountLabelsAsync(_uow, p.DestinationBankAccountId is int b ? new[] { b } : Array.Empty<int>(), ct);
            return ApiResponse<InvoicePaymentDto>.Ok(InvoicePaymentSupport.Map(p, inv, banks),
                status == PaymentRules.Confirmed
                    ? (inv.Status == "PAID" ? $"Payment {p.PaymentNumber} recorded. The invoice is now paid in full." : $"Payment {p.PaymentNumber} recorded. Balance due: {inv.BalanceAmount:0.00}")
                    : $"Payment {p.PaymentNumber} recorded as PENDING. It will reduce the balance once it is confirmed on the bank statement.");
        }
    }

    public class ConfirmInvoicePaymentHandler : IRequestHandler<ConfirmInvoicePaymentCommand, ApiResponse<InvoicePaymentDto>>
    {
        private readonly IUnitOfWork _uow;
        public ConfirmInvoicePaymentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<InvoicePaymentDto>> Handle(ConfirmInvoicePaymentCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.StatementReference))
                return ApiResponse<InvoicePaymentDto>.Fail("Enter the bank-statement line / reference this payment was matched to.");
            var p = await _uow.Repository<InvoicePayment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.PaymentId, ct);
            if (p == null) return ApiResponse<InvoicePaymentDto>.Fail("Payment not found.");
            if (p.Status != PaymentRules.Pending) return ApiResponse<InvoicePaymentDto>.Fail($"This payment is already {p.Status.ToLowerInvariant()}.");
            var inv = await _uow.Repository<Invoice>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == p.InvoiceId, ct);
            if (inv == null) return ApiResponse<InvoicePaymentDto>.Fail("Invoice not found.");
            if (inv.Status == "CANCELLED") return ApiResponse<InvoicePaymentDto>.Fail("The invoice was cancelled.");
            if (p.Amount > inv.BalanceAmount) return ApiResponse<InvoicePaymentDto>.Fail($"The invoice balance ({inv.BalanceAmount:0.00}) is now less than this payment.");

            p.Status = PaymentRules.Confirmed; p.ConfirmedAt = DateTime.UtcNow;
            p.ConfirmedByUserId = request.Actor.UserId; p.ConfirmedByName = request.Actor.Name;
            p.StatementReference = request.StatementReference.Trim();
            await _uow.Repository<InvoicePayment>().UpdateAsync(p, ct);
            InvoicePaymentSupport.Apply(inv, p.Amount);
            await _uow.Repository<Invoice>().UpdateAsync(inv, ct);
            var banks = await InvoicePaymentSupport.BankAccountLabelsAsync(_uow, p.DestinationBankAccountId is int b ? new[] { b } : Array.Empty<int>(), ct);
            return ApiResponse<InvoicePaymentDto>.Ok(InvoicePaymentSupport.Map(p, inv, banks), $"Payment {p.PaymentNumber} confirmed.");
        }
    }

    public class RejectInvoicePaymentHandler : IRequestHandler<RejectInvoicePaymentCommand, ApiResponse<InvoicePaymentDto>>
    {
        private readonly IUnitOfWork _uow;
        public RejectInvoicePaymentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<InvoicePaymentDto>> Handle(RejectInvoicePaymentCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Reason)) return ApiResponse<InvoicePaymentDto>.Fail("Give the reason this payment is rejected.");
            var p = await _uow.Repository<InvoicePayment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.PaymentId, ct);
            if (p == null) return ApiResponse<InvoicePaymentDto>.Fail("Payment not found.");
            if (p.Status != PaymentRules.Pending) return ApiResponse<InvoicePaymentDto>.Fail($"Only PENDING payments can be rejected (this one is {p.Status.ToLowerInvariant()}).");
            p.Status = PaymentRules.Rejected; p.RejectReason = request.Reason.Trim();
            p.ConfirmedByUserId = request.Actor.UserId; p.ConfirmedByName = request.Actor.Name; p.ConfirmedAt = DateTime.UtcNow;
            await _uow.Repository<InvoicePayment>().UpdateAsync(p, ct);
            return ApiResponse<InvoicePaymentDto>.Ok(InvoicePaymentSupport.Map(p), $"Payment {p.PaymentNumber} rejected.");
        }
    }

    public class GetInvoicePaymentsHandler : IRequestHandler<GetInvoicePaymentsQuery, ApiResponse<List<InvoicePaymentDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetInvoicePaymentsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<InvoicePaymentDto>>> Handle(GetInvoicePaymentsQuery request, CancellationToken ct)
        {
            var q = _uow.Repository<InvoicePayment>().TableNoTracking.Include(p => p.Invoice).Where(p => p.CompanyId == request.CompanyId);
            if (request.InvoiceId is > 0) q = q.Where(p => p.InvoiceId == request.InvoiceId);
            if (!string.IsNullOrWhiteSpace(request.Status)) { var s = request.Status.Trim().ToUpperInvariant(); q = q.Where(p => p.Status == s); }
            var list = await q.OrderByDescending(p => p.CreatedAt).Take(500).ToListAsync(ct);
            var banks = await InvoicePaymentSupport.BankAccountLabelsAsync(_uow, list.Where(p => p.DestinationBankAccountId != null).Select(p => p.DestinationBankAccountId!.Value), ct);
            return ApiResponse<List<InvoicePaymentDto>>.Ok(list.Select(p => InvoicePaymentSupport.Map(p, null, banks)).ToList());
        }
    }

    internal static class StringExt
    {
        public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
