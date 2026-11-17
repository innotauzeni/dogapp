using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.DogTraining.Commands;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Application.Features.DogTraining.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.DogTraining.Handlers
{
    // ── Training customers ───────────────────────────────────────────────────
    public class GetTrainingCustomersHandler : IRequestHandler<GetTrainingCustomersQuery, ApiResponse<List<TrainingCustomerDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetTrainingCustomersHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<TrainingCustomerDto>>> Handle(GetTrainingCustomersQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.CompanyId, ct);
            var q = _uow.Repository<DogTrainingCustomer>().TableNoTracking.Where(c => companyIds.Contains(c.CompanyId));
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                q = q.Where(c => c.FullName.Contains(s) || (c.Phone != null && c.Phone.Contains(s)) || (c.Email != null && c.Email.Contains(s)));
            }
            var list = await q.OrderBy(c => c.FullName).Take(1000).ToListAsync(ct);
            var ids = list.Select(c => c.Id).ToList();
            var dogCounts = await _uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => ids.Contains(d.CustomerId))
                .GroupBy(d => d.CustomerId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
            var enrCounts = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => e.CustomerId != null && ids.Contains(e.CustomerId.Value))
                .GroupBy(e => e.CustomerId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);

            return ApiResponse<List<TrainingCustomerDto>>.Ok(list.Select(c => new TrainingCustomerDto
            {
                Id = c.Id, CompanyId = c.CompanyId, FullName = c.FullName, Email = c.Email, Phone = c.Phone, Address = c.Address,
                IdNumber = c.IdNumber, Notes = c.Notes,
                DogCount = dogCounts.TryGetValue(c.Id, out var d) ? d : 0, EnrolmentCount = enrCounts.TryGetValue(c.Id, out var e) ? e : 0
            }).ToList());
        }
    }

    public class SaveTrainingCustomerHandler : IRequestHandler<SaveTrainingCustomerCommand, ApiResponse<TrainingCustomerDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveTrainingCustomerHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingCustomerDto>> Handle(SaveTrainingCustomerCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (string.IsNullOrWhiteSpace(d.FullName)) return ApiResponse<TrainingCustomerDto>.Fail("Customer name is required.");
            if (string.IsNullOrWhiteSpace(d.Phone) && string.IsNullOrWhiteSpace(d.Email))
                return ApiResponse<TrainingCustomerDto>.Fail("Enter a phone number or an email address so the customer can be contacted.");

            var repo = _uow.Repository<DogTrainingCustomer>();
            DogTrainingCustomer c;
            if (d.Id is > 0)
            {
                c = await repo.Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.Id.Value, ct) ?? null!;
                if (c == null) return ApiResponse<TrainingCustomerDto>.Fail("Customer not found.");
            }
            else c = new DogTrainingCustomer { CompanyId = d.CompanyId };

            c.FullName = d.FullName.Trim();
            c.Email = TrainingSupport.Trim(d.Email);
            c.Phone = TrainingSupport.Trim(d.Phone);
            c.Address = TrainingSupport.Trim(d.Address);
            c.IdNumber = TrainingSupport.Trim(d.IdNumber);
            c.Notes = TrainingSupport.Trim(d.Notes);
            if (c.Id == 0) await repo.AddAsync(c, ct); else await repo.UpdateAsync(c, ct);

            return ApiResponse<TrainingCustomerDto>.Ok(new TrainingCustomerDto
            {
                Id = c.Id, CompanyId = c.CompanyId, FullName = c.FullName, Email = c.Email, Phone = c.Phone, Address = c.Address, IdNumber = c.IdNumber, Notes = c.Notes
            }, "Customer saved.");
        }
    }

    public class DeleteTrainingCustomerHandler : IRequestHandler<DeleteTrainingCustomerCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteTrainingCustomerHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteTrainingCustomerCommand request, CancellationToken ct)
        {
            var c = await _uow.Repository<DogTrainingCustomer>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (c == null) return ApiResponse<bool>.Fail("Customer not found.");
            if (await _uow.Repository<DogTrainingDog>().TableNoTracking.AnyAsync(d => d.CustomerId == c.Id, ct))
                return ApiResponse<bool>.Fail("This customer still has dogs registered. Remove those first.");
            if (await _uow.Repository<DogTrainingInvoice>().TableNoTracking.AnyAsync(i => i.CustomerId == c.Id, ct))
                return ApiResponse<bool>.Fail("This customer has training invoices and cannot be deleted.");
            await _uow.Repository<DogTrainingCustomer>().DeleteAsync(c, ct);
            return ApiResponse<bool>.Ok(true, "Customer deleted.");
        }
    }

    // ── Training dogs ────────────────────────────────────────────────────────
    public class GetTrainingDogsHandler : IRequestHandler<GetTrainingDogsQuery, ApiResponse<List<TrainingDogDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetTrainingDogsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<TrainingDogDto>>> Handle(GetTrainingDogsQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.CompanyId, ct);
            var q = _uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => companyIds.Contains(d.CompanyId));
            if (request.CustomerId.HasValue) q = q.Where(d => d.CustomerId == request.CustomerId.Value);
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                q = q.Where(d => d.Name.Contains(s) || (d.Breed != null && d.Breed.Contains(s)) || (d.MicrochipNumber != null && d.MicrochipNumber.Contains(s)));
            }
            var list = await q.OrderBy(d => d.Name).Take(1000).ToListAsync(ct);
            var custIds = list.Select(d => d.CustomerId).Distinct().ToList();
            var customers = await _uow.Repository<DogTrainingCustomer>().TableNoTracking.Where(c => custIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.FullName, ct);
            var ids = list.Select(d => d.Id).ToList();
            var counts = await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => ids.Contains(e.DogId))
                .GroupBy(e => e.DogId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);

            return ApiResponse<List<TrainingDogDto>>.Ok(list.Select(d => new TrainingDogDto
            {
                Id = d.Id, CompanyId = d.CompanyId, CustomerId = d.CustomerId, CustomerName = customers.TryGetValue(d.CustomerId, out var cn) ? cn : null,
                Name = d.Name, Breed = d.Breed, Color = d.Color, Gender = d.Gender, DateOfBirth = d.DateOfBirth, MicrochipNumber = d.MicrochipNumber,
                PhotoUrl = d.PhotoUrl, Notes = d.Notes, EnrolmentCount = counts.TryGetValue(d.Id, out var n) ? n : 0
            }).ToList());
        }
    }

    public class SaveTrainingDogHandler : IRequestHandler<SaveTrainingDogCommand, ApiResponse<TrainingDogDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveTrainingDogHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingDogDto>> Handle(SaveTrainingDogCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (string.IsNullOrWhiteSpace(d.Name)) return ApiResponse<TrainingDogDto>.Fail("Dog name is required.");
            var customer = await _uow.Repository<DogTrainingCustomer>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == d.CustomerId, ct);
            if (customer == null) return ApiResponse<TrainingDogDto>.Fail("Choose the dog's owner (customer).");
            if (d.DateOfBirth.HasValue && d.DateOfBirth.Value.Date > DateTime.Today) return ApiResponse<TrainingDogDto>.Fail("Date of birth cannot be in the future.");

            var repo = _uow.Repository<DogTrainingDog>();
            DogTrainingDog dog;
            if (d.Id is > 0)
            {
                dog = await repo.Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.Id.Value, ct) ?? null!;
                if (dog == null) return ApiResponse<TrainingDogDto>.Fail("Dog not found.");
            }
            else dog = new DogTrainingDog { CompanyId = customer.CompanyId };

            dog.CustomerId = customer.Id;
            dog.Name = d.Name.Trim();
            dog.Breed = TrainingSupport.Trim(d.Breed);
            dog.Color = TrainingSupport.Trim(d.Color);
            dog.Gender = TrainingSupport.Trim(d.Gender);
            dog.DateOfBirth = d.DateOfBirth;
            dog.MicrochipNumber = TrainingSupport.Trim(d.MicrochipNumber);
            dog.PhotoUrl = TrainingSupport.Trim(d.PhotoUrl);
            dog.Notes = TrainingSupport.Trim(d.Notes);
            if (dog.Id == 0) await repo.AddAsync(dog, ct); else await repo.UpdateAsync(dog, ct);

            return ApiResponse<TrainingDogDto>.Ok(new TrainingDogDto
            {
                Id = dog.Id, CompanyId = dog.CompanyId, CustomerId = dog.CustomerId, CustomerName = customer.FullName, Name = dog.Name, Breed = dog.Breed,
                Color = dog.Color, Gender = dog.Gender, DateOfBirth = dog.DateOfBirth, MicrochipNumber = dog.MicrochipNumber, PhotoUrl = dog.PhotoUrl, Notes = dog.Notes
            }, "Dog saved.");
        }
    }

    public class DeleteTrainingDogHandler : IRequestHandler<DeleteTrainingDogCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteTrainingDogHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteTrainingDogCommand request, CancellationToken ct)
        {
            var dog = await _uow.Repository<DogTrainingDog>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (dog == null) return ApiResponse<bool>.Fail("Dog not found.");
            if (await _uow.Repository<DogTrainingEnrollment>().TableNoTracking.AnyAsync(e => e.DogId == dog.Id, ct))
                return ApiResponse<bool>.Fail("This dog has training enrolments and cannot be deleted.");
            await _uow.Repository<DogTrainingDog>().DeleteAsync(dog, ct);
            return ApiResponse<bool>.Ok(true, "Dog deleted.");
        }
    }

    // ── Training invoices & payments ─────────────────────────────────────────
    internal static class InvoiceMapper
    {
        public static async Task<List<TrainingInvoiceDto>> MapAsync(IUnitOfWork uow, List<DogTrainingInvoice> list, CancellationToken ct)
        {
            if (list.Count == 0) return new();
            var ids = list.Select(i => i.Id).ToList();
            var items = (await uow.Repository<DogTrainingInvoiceItem>().TableNoTracking.Where(x => ids.Contains(x.InvoiceId)).ToListAsync(ct))
                .GroupBy(x => x.InvoiceId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList());
            var pays = (await uow.Repository<DogTrainingPayment>().TableNoTracking.Where(x => ids.Contains(x.InvoiceId)).ToListAsync(ct))
                .GroupBy(x => x.InvoiceId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.PaymentDate).ThenBy(x => x.Id).ToList());
            var enrs = (await uow.Repository<DogTrainingEnrollment>().TableNoTracking.Where(e => e.InvoiceId != null && ids.Contains(e.InvoiceId.Value)).ToListAsync(ct))
                .GroupBy(e => e.InvoiceId!.Value).ToDictionary(g => g.Key, g => g.First());
            var dogIds = enrs.Values.Select(e => e.DogId).Distinct().ToList();
            var dogs = await uow.Repository<DogTrainingDog>().TableNoTracking.Where(d => dogIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, ct);

            var banks = await Invoices.Billing.InvoicePaymentSupport.BankAccountLabelsAsync(uow, pays.Values.SelectMany(x => x).Where(p => p.DestinationBankAccountId != null).Select(p => p.DestinationBankAccountId!.Value), ct);

            return list.Select(i =>
            {
                enrs.TryGetValue(i.Id, out var e);
                return new TrainingInvoiceDto
                {
                    Id = i.Id, CompanyId = i.CompanyId, InvoiceNumber = i.InvoiceNumber, EnrollmentId = e?.Id, EnrollmentNumber = e?.EnrollmentNumber,
                    DogName = e != null && dogs.TryGetValue(e.DogId, out var dn) ? dn : null, CustomerId = i.CustomerId, CustomerName = i.CustomerName,
                    CustomerEmail = i.CustomerEmail, CustomerPhone = i.CustomerPhone, InvoiceDate = i.InvoiceDate, DueDate = i.DueDate,
                    TotalAmount = i.TotalAmount, PaidAmount = i.PaidAmount, BalanceAmount = i.BalanceAmount, Status = i.Status, Notes = i.Notes, Terms = i.Terms,
                    Items = (items.TryGetValue(i.Id, out var its) ? its : new()).Select(x => new TrainingInvoiceItemDto { Description = x.Description, Quantity = x.Quantity, UnitPrice = x.UnitPrice, Amount = x.Amount }).ToList(),
                    Payments = (pays.TryGetValue(i.Id, out var ps) ? ps : new()).Select(p => new TrainingPaymentDto
                    {
                        Id = p.Id, PaymentNumber = p.PaymentNumber, PaymentDate = p.PaymentDate, Amount = p.Amount, Method = p.Method,
                        ReferenceNumber = p.ReferenceNumber, Notes = p.Notes, ReceivedByName = p.ReceivedByName,
                        ValueDate = p.ValueDate, CurrencyCode = p.CurrencyCode, Status = p.Status, MethodLabel = Common.Payments.PaymentRules.Find(p.Method)?.Label ?? p.Method,
                        PayerName = p.PayerName, PayerContact = p.PayerContact, PayerBank = p.PayerBank, PayerAccount = p.PayerAccount,
                        DestinationBankAccountId = p.DestinationBankAccountId,
                        DestinationAccountLabel = p.DestinationBankAccountId is int bid && banks.TryGetValue(bid, out var bl) ? bl : null,
                        ChequeNumber = p.ChequeNumber, ProofUrl = p.ProofUrl, ConfirmedByName = p.ConfirmedByName, ConfirmedAt = p.ConfirmedAt,
                        StatementReference = p.StatementReference, RejectReason = p.RejectReason
                    }).ToList(),
                    PendingAmount = (pays.TryGetValue(i.Id, out var pp) ? pp : new()).Where(p => p.Status == "PENDING").Sum(p => p.Amount),
                    EmailedAt = i.EmailedAt, EmailedTo = i.EmailedTo
                };
            }).ToList();
        }
    }

    public class GetTrainingInvoicesHandler : IRequestHandler<GetTrainingInvoicesQuery, ApiResponse<List<TrainingInvoiceDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetTrainingInvoicesHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<TrainingInvoiceDto>>> Handle(GetTrainingInvoicesQuery request, CancellationToken ct)
        {
            var companyIds = await TrainingSupport.CompanyScope(_uow, request.Actor.CompanyId, ct);
            var q = _uow.Repository<DogTrainingInvoice>().TableNoTracking.Where(i => companyIds.Contains(i.CompanyId));
            if (!string.IsNullOrWhiteSpace(request.Status)) q = q.Where(i => i.Status == request.Status);
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                q = q.Where(i => i.InvoiceNumber.Contains(s) || (i.CustomerName != null && i.CustomerName.Contains(s)));
            }
            var list = await q.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id).Take(500).ToListAsync(ct);
            return ApiResponse<List<TrainingInvoiceDto>>.Ok(await InvoiceMapper.MapAsync(_uow, list, ct));
        }
    }

    public class GetTrainingInvoiceByIdHandler : IRequestHandler<GetTrainingInvoiceByIdQuery, ApiResponse<TrainingInvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetTrainingInvoiceByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingInvoiceDto>> Handle(GetTrainingInvoiceByIdQuery request, CancellationToken ct)
        {
            var i = await _uow.Repository<DogTrainingInvoice>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (i == null) return ApiResponse<TrainingInvoiceDto>.Fail("Invoice not found.");
            return ApiResponse<TrainingInvoiceDto>.Ok((await InvoiceMapper.MapAsync(_uow, new List<DogTrainingInvoice> { i }, ct)).First());
        }
    }

    public class RecordTrainingPaymentHandler : IRequestHandler<RecordTrainingPaymentCommand, ApiResponse<TrainingInvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public RecordTrainingPaymentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingInvoiceDto>> Handle(RecordTrainingPaymentCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (!request.Actor.CanVerifyPayments) d.VerifiedOnStatement = false;
            var err = Common.Payments.PaymentRules.Validate(d);
            if (err != null) return ApiResponse<TrainingInvoiceDto>.Fail(err);
            var info = Common.Payments.PaymentRules.Find(d.Method)!;

            var inv = await _uow.Repository<DogTrainingInvoice>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.InvoiceId, ct);
            if (inv == null) return ApiResponse<TrainingInvoiceDto>.Fail("Invoice not found.");
            if (inv.Status == "CANCELLED") return ApiResponse<TrainingInvoiceDto>.Fail("This invoice was cancelled.");
            if (inv.Status == "PAID" || inv.BalanceAmount <= 0) return ApiResponse<TrainingInvoiceDto>.Fail("This invoice is already paid in full.");

            var pending = (await _uow.Repository<DogTrainingPayment>().TableNoTracking
                .Where(x => x.InvoiceId == inv.Id && x.Status == "PENDING").Select(x => x.Amount).ToListAsync(ct)).Sum();
            var open = inv.BalanceAmount - pending;
            if (d.Amount > open)
                return ApiResponse<TrainingInvoiceDto>.Fail(pending > 0
                    ? $"The amount is more than what is still open ({open:0.00}); {pending:0.00} is already waiting to be confirmed."
                    : $"The amount is more than the balance due ({inv.BalanceAmount:0.00}).");

            if (d.DestinationBankAccountId is > 0 &&
                !await _uow.Repository<BankAccount>().TableNoTracking.AnyAsync(b => b.Id == d.DestinationBankAccountId, ct))
                return ApiResponse<TrainingInvoiceDto>.Fail("The selected receiving bank account does not exist.");

            var reference = TrainingSupport.Trim(d.ReferenceNumber);
            if (!string.IsNullOrEmpty(reference) && info.Code != "CASH")
            {
                var low = reference.ToLower();
                var used = await _uow.Repository<DogTrainingPayment>().TableNoTracking
                    .Where(x => x.Status != "REJECTED" && x.ReferenceNumber != null && x.ReferenceNumber.ToLower() == low && x.Method != "CASH")
                    .Select(x => x.PaymentNumber).FirstOrDefaultAsync(ct);
                if (used != null) return ApiResponse<TrainingInvoiceDto>.Fail($"Reference '{reference}' was already used on payment {used}. Every transfer can be recorded only once.");
            }

            var status = Common.Payments.PaymentRules.InitialStatus(info, request.Actor.CanVerifyPayments, d.VerifiedOnStatement);
            static string? N(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            var p = new DogTrainingPayment
            {
                CompanyId = inv.CompanyId, InvoiceId = inv.Id,
                PaymentNumber = await TrainingSupport.NextPaymentNumberAsync(_uow, ct),
                PaymentDate = DateTime.UtcNow, ValueDate = d.ValueDate ?? d.PaymentDate, Amount = d.Amount, Method = info.Code, Status = status,
                CurrencyCode = string.IsNullOrWhiteSpace(d.CurrencyCode) ? "USD" : d.CurrencyCode.Trim().ToUpperInvariant(),
                ReferenceNumber = N(reference), Notes = N(d.Notes), ChequeNumber = N(d.ChequeNumber), ProofUrl = N(d.ProofUrl),
                PayerName = N(d.PayerName), PayerContact = N(d.PayerContact), PayerBank = N(d.PayerBank), PayerAccount = N(d.PayerAccount),
                DestinationBankAccountId = d.DestinationBankAccountId is > 0 ? d.DestinationBankAccountId : null,
                ReceivedByUserId = request.Actor.UserId, ReceivedByName = request.Actor.Name,
            };
            if (status == "CONFIRMED")
            {
                p.ConfirmedByUserId = request.Actor.UserId; p.ConfirmedByName = request.Actor.Name; p.ConfirmedAt = DateTime.UtcNow;
                p.StatementReference = N(d.StatementReference);
            }
            await _uow.Repository<DogTrainingPayment>().AddAsync(p, ct);

            if (status == "CONFIRMED")
            {
                inv.PaidAmount += d.Amount;
                inv.BalanceAmount = inv.TotalAmount - inv.PaidAmount;
                inv.Status = inv.BalanceAmount <= 0 ? "PAID" : "PARTIAL";
                await _uow.Repository<DogTrainingInvoice>().UpdateAsync(inv, ct);
                // A fully paid invoice unlocks handler allocation straight away.
                await TrainingSupport.SyncPaymentAsync(_uow, null, null, ct, inv.Id);
            }

            var fresh = await _uow.Repository<DogTrainingInvoice>().TableNoTracking.FirstAsync(x => x.Id == inv.Id, ct);
            return ApiResponse<TrainingInvoiceDto>.Ok((await InvoiceMapper.MapAsync(_uow, new List<DogTrainingInvoice> { fresh }, ct)).First(),
                status != "CONFIRMED" ? $"Payment {p.PaymentNumber} recorded as PENDING. It will reduce the balance once it is confirmed on the bank statement."
                : fresh.Status == "PAID" ? "Payment recorded. The invoice is now paid in full." : $"Payment recorded. Balance due: {fresh.BalanceAmount:0.00}");
        }
    }

    public class ConfirmTrainingPaymentHandler : IRequestHandler<ConfirmTrainingPaymentCommand, ApiResponse<TrainingInvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public ConfirmTrainingPaymentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingInvoiceDto>> Handle(ConfirmTrainingPaymentCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.StatementReference))
                return ApiResponse<TrainingInvoiceDto>.Fail("Enter the bank-statement line / reference this payment was matched to.");
            var p = await _uow.Repository<DogTrainingPayment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.PaymentId, ct);
            if (p == null) return ApiResponse<TrainingInvoiceDto>.Fail("Payment not found.");
            if (p.Status != "PENDING") return ApiResponse<TrainingInvoiceDto>.Fail($"This payment is already {p.Status.ToLowerInvariant()}.");
            var inv = await _uow.Repository<DogTrainingInvoice>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == p.InvoiceId, ct);
            if (inv == null) return ApiResponse<TrainingInvoiceDto>.Fail("Invoice not found.");
            if (inv.Status == "CANCELLED") return ApiResponse<TrainingInvoiceDto>.Fail("The invoice was cancelled.");
            if (p.Amount > inv.BalanceAmount) return ApiResponse<TrainingInvoiceDto>.Fail($"The invoice balance ({inv.BalanceAmount:0.00}) is now less than this payment.");

            p.Status = "CONFIRMED"; p.ConfirmedAt = DateTime.UtcNow; p.ConfirmedByUserId = request.Actor.UserId; p.ConfirmedByName = request.Actor.Name;
            p.StatementReference = request.StatementReference.Trim();
            await _uow.Repository<DogTrainingPayment>().UpdateAsync(p, ct);
            inv.PaidAmount += p.Amount;
            inv.BalanceAmount = inv.TotalAmount - inv.PaidAmount;
            inv.Status = inv.BalanceAmount <= 0 ? "PAID" : "PARTIAL";
            await _uow.Repository<DogTrainingInvoice>().UpdateAsync(inv, ct);
            await TrainingSupport.SyncPaymentAsync(_uow, null, null, ct, inv.Id);

            var fresh = await _uow.Repository<DogTrainingInvoice>().TableNoTracking.FirstAsync(x => x.Id == inv.Id, ct);
            return ApiResponse<TrainingInvoiceDto>.Ok((await InvoiceMapper.MapAsync(_uow, new List<DogTrainingInvoice> { fresh }, ct)).First(), $"Payment {p.PaymentNumber} confirmed.");
        }
    }

    public class RejectTrainingPaymentHandler : IRequestHandler<RejectTrainingPaymentCommand, ApiResponse<TrainingInvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public RejectTrainingPaymentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingInvoiceDto>> Handle(RejectTrainingPaymentCommand request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Reason)) return ApiResponse<TrainingInvoiceDto>.Fail("Give the reason this payment is rejected.");
            var p = await _uow.Repository<DogTrainingPayment>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.PaymentId, ct);
            if (p == null) return ApiResponse<TrainingInvoiceDto>.Fail("Payment not found.");
            if (p.Status != "PENDING") return ApiResponse<TrainingInvoiceDto>.Fail($"Only PENDING payments can be rejected (this one is {p.Status.ToLowerInvariant()}).");
            p.Status = "REJECTED"; p.RejectReason = request.Reason.Trim(); p.ConfirmedAt = DateTime.UtcNow; p.ConfirmedByUserId = request.Actor.UserId; p.ConfirmedByName = request.Actor.Name;
            await _uow.Repository<DogTrainingPayment>().UpdateAsync(p, ct);
            var fresh = await _uow.Repository<DogTrainingInvoice>().TableNoTracking.FirstAsync(x => x.Id == p.InvoiceId, ct);
            return ApiResponse<TrainingInvoiceDto>.Ok((await InvoiceMapper.MapAsync(_uow, new List<DogTrainingInvoice> { fresh }, ct)).First(), $"Payment {p.PaymentNumber} rejected.");
        }
    }

    public class CancelTrainingInvoiceHandler : IRequestHandler<CancelTrainingInvoiceCommand, ApiResponse<TrainingInvoiceDto>>
    {
        private readonly IUnitOfWork _uow;
        public CancelTrainingInvoiceHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<TrainingInvoiceDto>> Handle(CancelTrainingInvoiceCommand request, CancellationToken ct)
        {
            var inv = await _uow.Repository<DogTrainingInvoice>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.InvoiceId, ct);
            if (inv == null) return ApiResponse<TrainingInvoiceDto>.Fail("Invoice not found.");
            if (inv.PaidAmount > 0) return ApiResponse<TrainingInvoiceDto>.Fail("An invoice with payments recorded cannot be cancelled.");
            if (inv.Status == "CANCELLED") return ApiResponse<TrainingInvoiceDto>.Fail("Already cancelled.");
            inv.Status = "CANCELLED";
            await _uow.Repository<DogTrainingInvoice>().UpdateAsync(inv, ct);
            await TrainingSupport.SyncPaymentAsync(_uow, null, null, ct, inv.Id); // sends the enrolment back so a new invoice can be raised
            var fresh = await _uow.Repository<DogTrainingInvoice>().TableNoTracking.FirstAsync(x => x.Id == inv.Id, ct);
            return ApiResponse<TrainingInvoiceDto>.Ok((await InvoiceMapper.MapAsync(_uow, new List<DogTrainingInvoice> { fresh }, ct)).First(), "Invoice cancelled.");
        }
    }
}
