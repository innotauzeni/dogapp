using Dog.Application.Contracts;
using Dog.Application.Specifications;
using Dog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dog.Infrastructure.Repositories
{

    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext db)
        {
            _db = db;
            _dbSet = db.Set<T>();
        }

        public IQueryable<T> Table => _dbSet;
        public IQueryable<T> TableNoTracking => _dbSet.AsNoTracking();

        public async Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new[] { id }, cancellationToken);
        }

        public async Task<List<T>> ListAsync(BaseSpecification<T> spec, CancellationToken cancellationToken = default)
        {
            var query = SpecificationEvaluator<T>.GetQuery(_dbSet.AsQueryable(), spec);
            return await query.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<T> GetEntityWithSpecAsync(BaseSpecification<T> spec, CancellationToken cancellationToken = default)
        {
            var query = SpecificationEvaluator<T>.GetQuery(_dbSet.AsQueryable(), spec);
            return await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> CountAsync(BaseSpecification<T> spec, CancellationToken cancellationToken = default)
        {
            var query = SpecificationEvaluator<T>.GetQuery(_dbSet.AsQueryable(), spec);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<int> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return await _db.SaveChangesAsync(cancellationToken);

        }

        public async Task<int> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddRangeAsync(entities, cancellationToken);
            return await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            return await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            return await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            _dbSet.RemoveRange(entities);
            return await _db.SaveChangesAsync(cancellationToken);
        }
    }

}
