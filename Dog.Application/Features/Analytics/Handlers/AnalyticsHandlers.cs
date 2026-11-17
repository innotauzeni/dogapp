using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Analytics.DTOs;
using Dog.Application.Features.Analytics.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Analytics.Handlers
{
    public class GetAnalyticsSummaryHandler : IRequestHandler<GetAnalyticsSummaryQuery, ApiResponse<AnalyticsSummaryDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetAnalyticsSummaryHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<AnalyticsSummaryDto>> Handle(GetAnalyticsSummaryQuery request, CancellationToken ct)
        {
            var companyIds = await CompanyScopeResolver.ResolveAsync(_uow, request.CompanyId, ct);
            var months = request.Months <= 0 ? 6 : request.Months;
            var since = DateTime.UtcNow.Date.AddMonths(-(months - 1)).AddDays(1 - DateTime.UtcNow.Day);
            var now = DateTime.UtcNow;

            var dto = new AnalyticsSummaryDto();

            // ── Dogs ─────────────────────────────────────────────────────────
            var dogs = await _uow.Repository<Dogtbl>().TableNoTracking
                .Where(d => companyIds.Contains(d.CompanyId))
                .ToListAsync(ct);
            dto.TotalDogs = dogs.Count;
            dto.ActiveDogs = dogs.Count(d => d.IsActive);

            dto.DeployedDogs = await _uow.Repository<Deployment>().TableNoTracking
                .Where(dep => companyIds.Contains(dep.CompanyId) && dep.IsActive && (dep.EndDate == null || dep.EndDate > now))
                .Select(dep => dep.DogId)
                .Distinct()
                .CountAsync(ct);

            dto.VaccinatedDogs = await _uow.Repository<VaccinationRecord>().TableNoTracking
                .Where(v => companyIds.Contains(v.CompanyId) && v.DogId != null)
                .Select(v => v.DogId!.Value)
                .Distinct()
                .CountAsync(ct);

            // ── Revenue (Invoices) ──────────────────────────────────────────
            var invoices = await _uow.Repository<Invoice>().TableNoTracking
                .Where(i => companyIds.Contains(i.CompanyId))
                .ToListAsync(ct);
            dto.TotalRevenue = invoices.Sum(i => i.PaidAmount);
            dto.TotalOutstanding = invoices.Where(i => i.Status != "PAID" && i.Status != "CANCELLED").Sum(i => i.BalanceAmount);

            dto.RevenueByMonth = invoices
                .Where(i => i.InvoiceDate >= since)
                .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new MonthlyPointDto
                {
                    Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Amount = g.Sum(i => i.TotalAmount),
                })
                .OrderBy(p => p.Month)
                .ToList();
            FillMissingMonths(dto.RevenueByMonth, since, months);

            // ── Expenses ─────────────────────────────────────────────────────
            var expenses = await _uow.Repository<Expense>().TableNoTracking
                .Where(e => companyIds.Contains(e.CompanyId))
                .ToListAsync(ct);
            dto.TotalExpenses = expenses.Sum(e => e.Amount);
            dto.NetProfit = dto.TotalRevenue - dto.TotalExpenses;

            dto.ExpensesByMonth = expenses
                .Where(e => e.ExpenseDate >= since)
                .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                .Select(g => new MonthlyPointDto
                {
                    Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Amount = g.Sum(e => e.Amount),
                })
                .OrderBy(p => p.Month)
                .ToList();
            FillMissingMonths(dto.ExpensesByMonth, since, months);

            dto.ExpensesByCategory = expenses
                .GroupBy(e => e.Category)
                .Select(g => new CategoryBreakdownDto { Category = g.Key, Amount = g.Sum(e => e.Amount) })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // ── Handler activity (last 30 days) ────────────────────────────
            var recentCutoff = now.AddDays(-30);
            var recentActivity = await _uow.Repository<DogCareRecord>().TableNoTracking
                .Where(c => companyIds.Contains(c.CompanyId) && c.OccurredOn >= recentCutoff && c.RecordedByUserId != null)
                .GroupBy(c => c.RecordedByUserId)
                .Select(g => new { UserId = g.Key!, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync(ct);

            if (recentActivity.Count > 0)
            {
                var userIds = recentActivity.Select(a => a.UserId).ToList();
                var users = await _uow.Repository<ApplicationUser>().TableNoTracking
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => $"{u.Name} {u.Surname}".Trim(), ct);

                dto.HandlerActivity = recentActivity
                    .Select(a => new HandlerActivityStatDto
                    {
                        HandlerName = users.TryGetValue(a.UserId, out var name) && !string.IsNullOrWhiteSpace(name) ? name : "Unknown",
                        ActivityCount = a.Count,
                    })
                    .ToList();
            }

            return ApiResponse<AnalyticsSummaryDto>.Ok(dto);
        }

        private static void FillMissingMonths(List<MonthlyPointDto> points, DateTime since, int months)
        {
            for (var i = 0; i < months; i++)
            {
                var month = since.AddMonths(i);
                var key = $"{month.Year:D4}-{month.Month:D2}";
                if (!points.Any(p => p.Month == key))
                {
                    points.Add(new MonthlyPointDto { Month = key, Label = month.ToString("MMM yyyy"), Amount = 0 });
                }
            }
            points.Sort((a, b) => string.Compare(a.Month, b.Month, StringComparison.Ordinal));
        }
    }
}
