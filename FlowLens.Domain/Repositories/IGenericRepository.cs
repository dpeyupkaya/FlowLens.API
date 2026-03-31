using System.Linq.Expressions;
using FlowLens.Domain.Common;

namespace FlowLens.Domain.Repositories;

public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    Task AddAsync(TEntity entity);
    Task AddRangeAsync(List<TEntity> entities);
    Task RemoveAsync(Guid id);
    Task RemoveRangeAsync(List<Guid> ids);
    Task UpdateAsync(TEntity entity);
    Task<List<TEntity>> GetAllAsync();
    Task<TEntity> GetByIdAsync(Guid id);
    Task<TEntity> GetSingleAsync(Expression<Func<TEntity, bool>> filter);
    Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> filter);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
}