using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Customers.DTOs;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.CustomerCompanies
{
    public class CustomerCompanyDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TradingName { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? TaxNumber { get; set; }
        public string? VatNumber { get; set; }
        public string? Email { get; set; }
        public string? BillingEmail { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? ContactPerson { get; set; }
        public int PaymentTermsDays { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public int ContactCount { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
    }

    public class CustomerCompanyProfileDto : CustomerCompanyDto
    {
        public List<CustomerDto> Contacts { get; set; } = new();
        public List<CompanyInvoiceRowDto> Invoices { get; set; } = new();
    }

    public class CompanyInvoiceRowDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SaveCustomerCompanyDto
    {
        public int? Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TradingName { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? TaxNumber { get; set; }
        public string? VatNumber { get; set; }
        public string? Email { get; set; }
        public string? BillingEmail { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? ContactPerson { get; set; }
        public int PaymentTermsDays { get; set; } = 30;
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public record GetCustomerCompaniesQuery(int CompanyId, string? Search) : IRequest<ApiResponse<List<CustomerCompanyDto>>>;
    public record GetCustomerCompanyProfileQuery(int Id) : IRequest<ApiResponse<CustomerCompanyProfileDto>>;
    public record SaveCustomerCompanyCommand(SaveCustomerCompanyDto Dto) : IRequest<ApiResponse<CustomerCompanyDto>>;
    public record DeleteCustomerCompanyCommand(int Id) : IRequest<ApiResponse<bool>>;

    internal static class CustomerCompanyMapper
    {
        public static CustomerCompanyDto Map(CustomerCompany c) => new()
        {
            Id = c.Id, CompanyId = c.CompanyId, Name = c.Name, TradingName = c.TradingName, RegistrationNumber = c.RegistrationNumber,
            TaxNumber = c.TaxNumber, VatNumber = c.VatNumber, Email = c.Email, BillingEmail = c.BillingEmail, Phone = c.Phone, Website = c.Website,
            Address = c.Address, City = c.City, Country = c.Country, ContactPerson = c.ContactPerson, PaymentTermsDays = c.PaymentTermsDays,
            Notes = c.Notes, IsActive = c.IsActive,
        };
    }

    public class GetCustomerCompaniesHandler : IRequestHandler<GetCustomerCompaniesQuery, ApiResponse<List<CustomerCompanyDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetCustomerCompaniesHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<CustomerCompanyDto>>> Handle(GetCustomerCompaniesQuery request, CancellationToken ct)
        {
            var q = _uow.Repository<CustomerCompany>().TableNoTracking.Where(c => c.CompanyId == request.CompanyId);
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                q = q.Where(c => c.Name.Contains(s) || (c.TradingName != null && c.TradingName.Contains(s)) || (c.VatNumber != null && c.VatNumber.Contains(s))
                    || (c.TaxNumber != null && c.TaxNumber.Contains(s)) || (c.RegistrationNumber != null && c.RegistrationNumber.Contains(s)) || (c.Email != null && c.Email.Contains(s)));
            }
            var list = await q.OrderBy(c => c.Name).Take(1000).ToListAsync(ct);
            var ids = list.Select(c => c.Id).ToList();

            var contactRows = await _uow.Repository<Customer>().TableNoTracking
                .Where(x => x.CustomerCompanyId != null && ids.Contains(x.CustomerCompanyId.Value))
                .Select(x => x.CustomerCompanyId!.Value).ToListAsync(ct);
            var contacts = contactRows.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

            var inv = (await _uow.Repository<Invoice>().TableNoTracking
                    .Where(i => i.CustomerCompanyId != null && ids.Contains(i.CustomerCompanyId.Value) && i.Status != "CANCELLED" && i.Status != "DRAFT")
                    .Select(i => new { Co = i.CustomerCompanyId!.Value, i.TotalAmount, i.PaidAmount, i.BalanceAmount }).ToListAsync(ct))
                .GroupBy(i => i.Co).ToDictionary(g => g.Key, g => new { N = g.Count(), Total = g.Sum(x => x.TotalAmount), Paid = g.Sum(x => x.PaidAmount), Bal = g.Sum(x => x.BalanceAmount) });

            return ApiResponse<List<CustomerCompanyDto>>.Ok(list.Select(c =>
            {
                var d = CustomerCompanyMapper.Map(c);
                d.ContactCount = contacts.TryGetValue(c.Id, out var n) ? n : 0;
                if (inv.TryGetValue(c.Id, out var t)) { d.InvoiceCount = t.N; d.TotalBilled = t.Total; d.TotalPaid = t.Paid; d.Outstanding = t.Bal; }
                return d;
            }).ToList());
        }
    }

    public class GetCustomerCompanyProfileHandler : IRequestHandler<GetCustomerCompanyProfileQuery, ApiResponse<CustomerCompanyProfileDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetCustomerCompanyProfileHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CustomerCompanyProfileDto>> Handle(GetCustomerCompanyProfileQuery request, CancellationToken ct)
        {
            var c = await _uow.Repository<CustomerCompany>().TableNoTracking.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (c == null) return ApiResponse<CustomerCompanyProfileDto>.Fail("Company not found.");

            var contacts = await _uow.Repository<Customer>().TableNoTracking.Where(x => x.CustomerCompanyId == c.Id).OrderBy(x => x.FullName).ToListAsync(ct);
            var invoices = await _uow.Repository<Invoice>().TableNoTracking.Where(i => i.CustomerCompanyId == c.Id).OrderByDescending(i => i.InvoiceDate).Take(200).ToListAsync(ct);
            var live = invoices.Where(i => i.Status != "CANCELLED" && i.Status != "DRAFT").ToList();

            var p = new CustomerCompanyProfileDto
            {
                Id = c.Id, CompanyId = c.CompanyId, Name = c.Name, TradingName = c.TradingName, RegistrationNumber = c.RegistrationNumber, TaxNumber = c.TaxNumber,
                VatNumber = c.VatNumber, Email = c.Email, BillingEmail = c.BillingEmail, Phone = c.Phone, Website = c.Website, Address = c.Address, City = c.City,
                Country = c.Country, ContactPerson = c.ContactPerson, PaymentTermsDays = c.PaymentTermsDays, Notes = c.Notes, IsActive = c.IsActive,
                ContactCount = contacts.Count, InvoiceCount = live.Count, TotalBilled = live.Sum(i => i.TotalAmount), TotalPaid = live.Sum(i => i.PaidAmount), Outstanding = live.Sum(i => i.BalanceAmount),
                Contacts = contacts.Select(x => new CustomerDto
                {
                    Id = x.Id, CompanyId = x.CompanyId, FirstName = x.FirstName, LastName = x.LastName, FullName = x.FullName, Cellphone = x.Cellphone, Email = x.Email,
                    Address = x.Address, IDnumber = x.IDnumber, Gender = x.Gender, JobTitle = x.JobTitle, CustomerCompanyId = x.CustomerCompanyId, CustomerCompanyName = c.Name
                }).ToList(),
                Invoices = invoices.Select(i => new CompanyInvoiceRowDto
                {
                    Id = i.Id, InvoiceNumber = i.InvoiceNumber, InvoiceDate = i.InvoiceDate, DueDate = i.DueDate, TotalAmount = i.TotalAmount,
                    PaidAmount = i.PaidAmount, BalanceAmount = i.BalanceAmount, Status = i.Status
                }).ToList(),
            };
            return ApiResponse<CustomerCompanyProfileDto>.Ok(p);
        }
    }

    public class SaveCustomerCompanyHandler : IRequestHandler<SaveCustomerCompanyCommand, ApiResponse<CustomerCompanyDto>>
    {
        private readonly IUnitOfWork _uow;
        public SaveCustomerCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<CustomerCompanyDto>> Handle(SaveCustomerCompanyCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            if (string.IsNullOrWhiteSpace(d.Name)) return ApiResponse<CustomerCompanyDto>.Fail("The company name is required.");
            if (d.PaymentTermsDays is < 0 or > 365) return ApiResponse<CustomerCompanyDto>.Fail("Payment terms must be between 0 and 365 days.");
            if (!string.IsNullOrWhiteSpace(d.Email) && !d.Email.Contains('@')) return ApiResponse<CustomerCompanyDto>.Fail("The company email address is not valid.");

            var repo = _uow.Repository<CustomerCompany>();
            var name = d.Name.Trim();
            var vat = string.IsNullOrWhiteSpace(d.VatNumber) ? null : d.VatNumber.Trim();
            var reg = string.IsNullOrWhiteSpace(d.RegistrationNumber) ? null : d.RegistrationNumber.Trim();
            var selfId = d.Id ?? 0;
            var dup = await repo.TableNoTracking.AnyAsync(c => c.CompanyId == d.CompanyId && c.Id != selfId &&
                (c.Name == name || (vat != null && c.VatNumber == vat) || (reg != null && c.RegistrationNumber == reg)), ct);
            if (dup) return ApiResponse<CustomerCompanyDto>.Fail("A company with the same name, VAT number or registration number already exists.");

            CustomerCompany c;
            if (d.Id is > 0)
            {
                var existing = await repo.Table.AsTracking().FirstOrDefaultAsync(x => x.Id == d.Id.Value, ct);
                if (existing == null) return ApiResponse<CustomerCompanyDto>.Fail("Company not found.");
                c = existing;
            }
            else c = new CustomerCompany { CompanyId = d.CompanyId };

            static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            c.Name = name; c.TradingName = T(d.TradingName); c.RegistrationNumber = reg; c.TaxNumber = T(d.TaxNumber); c.VatNumber = vat;
            c.Email = T(d.Email); c.BillingEmail = T(d.BillingEmail); c.Phone = T(d.Phone); c.Website = T(d.Website); c.Address = T(d.Address); c.City = T(d.City);
            c.Country = T(d.Country); c.ContactPerson = T(d.ContactPerson); c.PaymentTermsDays = d.PaymentTermsDays; c.Notes = T(d.Notes); c.IsActive = d.IsActive;
            if (c.Id == 0) await repo.AddAsync(c, ct); else await repo.UpdateAsync(c, ct);
            return ApiResponse<CustomerCompanyDto>.Ok(CustomerCompanyMapper.Map(c), "Company saved.");
        }
    }

    public class DeleteCustomerCompanyHandler : IRequestHandler<DeleteCustomerCompanyCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteCustomerCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteCustomerCompanyCommand request, CancellationToken ct)
        {
            var c = await _uow.Repository<CustomerCompany>().Table.AsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (c == null) return ApiResponse<bool>.Fail("Company not found.");
            if (await _uow.Repository<Invoice>().TableNoTracking.AnyAsync(i => i.CustomerCompanyId == c.Id, ct))
                return ApiResponse<bool>.Fail("This company has invoices and cannot be deleted. Mark it inactive instead.");

            var contacts = await _uow.Repository<Customer>().Table.AsTracking().Where(x => x.CustomerCompanyId == c.Id).ToListAsync(ct);
            foreach (var x in contacts) { x.CustomerCompanyId = null; await _uow.Repository<Customer>().UpdateAsync(x, ct); }
            await _uow.Repository<CustomerCompany>().DeleteAsync(c, ct);
            return ApiResponse<bool>.Ok(true, "Company deleted.");
        }
    }
}
