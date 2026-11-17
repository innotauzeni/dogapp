using Dog.Application.Contracts;
using Dog.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Common
{
    /// Resolves which company IDs a company-scoped query should include.
    /// A head-office company (ParentCompanyId == null) sees itself plus all of
    /// its active branches; a branch only ever sees itself. This lets a head
    /// office user's default (no branch switched) view aggregate every branch,
    /// while switching to a specific branch narrows back down to just that one.
    public static class CompanyScopeResolver
    {
        public static async Task<List<int>> ResolveAsync(IUnitOfWork uow, int companyId, CancellationToken ct)
        {
            var company = await uow.Repository<Company>()
                .TableNoTracking
                .Include(c => c.Branches)
                .FirstOrDefaultAsync(c => c.Id == companyId, ct);

            if (company == null || company.ParentCompanyId != null)
                return new List<int> { companyId };

            var ids = new List<int> { companyId };
            ids.AddRange(company.Branches.Where(b => b.IsActive).Select(b => b.Id));
            return ids;
        }
    }
}
