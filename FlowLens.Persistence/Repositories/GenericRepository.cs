using FlowLens.Domain.Common;
using FlowLens.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowLens.Persistence.Repositories;

public class GenericRepository<TEntity, TContext> : IGenericRepository<TEntity>
    where TEntity : BaseEntity
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly DbSet<TEntity> _entity;

    public GenericRepository(TContext context)
    {
        _context = context;
        _entity = _context.Set<TEntity>();
    }

    public async Task AddAsync(TEntity entity)
    {
        await _context.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(List<TEntity> entities)
    {
        await _context.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid id)
    {
        var entity = await _entity.FindAsync(id);
        if (entity == null || entity.IsDeleted) return;

        if (await HasRelatedDataAsync(entity))
            throw new InvalidOperationException("Bu kayda bağlı aktif veriler olduğu için silinemez.");

        entity.IsDeleted = true;
        entity.DeletedDate = DateTimeOffset.UtcNow;
        entity.UpdatedDate = DateTimeOffset.UtcNow;

        _entity.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveRangeAsync(List<Guid> ids)
    {
        var entities = await _entity.Where(e => ids.Contains(e.Id) && !e.IsDeleted).ToListAsync();
        if (!entities.Any()) return;

        foreach (var entity in entities)
        {
            if (await HasRelatedDataAsync(entity))
                throw new InvalidOperationException($"{entity.Id} kaydına bağlı veriler var.");

            entity.IsDeleted = true;
            entity.DeletedDate = DateTimeOffset.UtcNow;
            entity.UpdatedDate = DateTimeOffset.UtcNow;
        }

        _entity.UpdateRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TEntity entity)
    {
        entity.UpdatedDate = DateTimeOffset.UtcNow;
        _entity.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TEntity>> GetAllAsync()
    {
        return await _entity.AsNoTracking().Where(e => !e.IsDeleted).ToListAsync();
    }

    public async Task<TEntity> GetByIdAsync(Guid id)
    {
        return await _entity.Where(e => !e.IsDeleted && e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<TEntity> GetSingleAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await _entity.Where(e => !e.IsDeleted).FirstOrDefaultAsync(filter);
    }

    public async Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await _entity.Where(e => !e.IsDeleted).Where(filter).ToListAsync();
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _entity.Where(e => !e.IsDeleted).AnyAsync(predicate);
    }

    private async Task<bool> HasRelatedDataAsync(TEntity entity)
    {
        var entry = _context.Entry(entity);

        foreach (var collection in entry.Collections)
        {
            await collection.LoadAsync();

            var hasActive = collection.CurrentValue?
                .Cast<object>()
                .Any(e => e is BaseEntity be && !be.IsDeleted);

            if (hasActive == true) return true;
        }

        return false;
    }
}