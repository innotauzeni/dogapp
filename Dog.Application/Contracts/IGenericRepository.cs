using Dog.Application.Specifications;

namespace Dog.Application.Contracts
{
    public interface IGenericRepository<T> where T : class
    {
        IQueryable<T> Table { get; }
        IQueryable<T> TableNoTracking { get; }

        Task<T> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        Task<List<T>> ListAsync(BaseSpecification<T> spec, CancellationToken cancellationToken = default);
        Task<T> GetEntityWithSpecAsync(BaseSpecification<T> spec, CancellationToken cancellationToken = default);
        Task<int> CountAsync(BaseSpecification<T> spec, CancellationToken cancellationToken = default);

        Task<int> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task<int> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default);

        Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    }

}
