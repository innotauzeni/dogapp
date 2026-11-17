using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Search.DTOs;
using Dog.Application.Features.Search.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Search.Handlers
{
    public class GlobalSearchHandler : IRequestHandler<GlobalSearchQuery, ApiResponse<SearchResultsDto>>
    {
        private readonly IUnitOfWork _uow;
        public GlobalSearchHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<SearchResultsDto>> Handle(GlobalSearchQuery request, CancellationToken ct)
        {
            var result = new SearchResultsDto();
            var q = request.Query?.Trim();
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return ApiResponse<SearchResultsDto>.Ok(result);

            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, request.CompanyId, ct);

            var dogs = await _uow.Repository<Dogtbl>()
                .TableNoTracking
                .Where(d => companyIds.Contains(d.CompanyId) &&
                    (d.Name.Contains(q) || d.Breed.Contains(q) || (d.MicrochipNumber != null && d.MicrochipNumber.Contains(q))))
                .OrderBy(d => d.Name)
                .Take(6)
                .Select(d => new SearchResultItemDto { Type = "Dog", Id = d.Id, Title = d.Name, Subtitle = d.Breed })
                .ToListAsync(ct);
            result.Dogs = dogs;

            var customers = await _uow.Repository<Customer>()
                .TableNoTracking
                .Where(c => companyIds.Contains(c.CompanyId) &&
                    ((c.FullName != null && c.FullName.Contains(q)) ||
                     (c.FirstName != null && c.FirstName.Contains(q)) ||
                     (c.LastName != null && c.LastName.Contains(q)) ||
                     (c.Email != null && c.Email.Contains(q)) ||
                     (c.Cellphone != null && c.Cellphone.Contains(q))))
                .OrderBy(c => c.FullName)
                .Take(6)
                .Select(c => new SearchResultItemDto
                {
                    Type = "Customer",
                    Id = c.Id,
                    Title = c.FullName ?? $"{c.FirstName} {c.LastName}",
                    Subtitle = c.Cellphone ?? c.Email
                })
                .ToListAsync(ct);
            result.Customers = customers;

            var invoices = await _uow.Repository<Invoice>()
                .TableNoTracking
                .Where(i => companyIds.Contains(i.CompanyId) &&
                    (i.InvoiceNumber.Contains(q) || (i.CustomerName != null && i.CustomerName.Contains(q))))
                .OrderByDescending(i => i.CreatedAt)
                .Take(6)
                .Select(i => new SearchResultItemDto
                {
                    Type = "Invoice",
                    Id = i.Id,
                    Title = i.InvoiceNumber,
                    Subtitle = i.CustomerName != null ? $"{i.CustomerName} — {i.TotalAmount:0.00}" : $"{i.TotalAmount:0.00}"
                })
                .ToListAsync(ct);
            result.Invoices = invoices;

            return ApiResponse<SearchResultsDto>.Ok(result);
        }
    }
}
