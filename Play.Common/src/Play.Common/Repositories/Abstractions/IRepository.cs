using System.Linq.Expressions;
using Play.Common.Entities.Abstractions;

namespace Play.Common.Repositories.Abstractions;

public interface IRepository<TEntity>
    where TEntity : class, IEntity
{
    Task<IReadOnlyCollection<TEntity>> GetAllAsync();
    Task<IReadOnlyCollection<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filter);
    Task<TEntity> GetAsync(Guid id);
    Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> filter);
    Task CreateAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(Guid id);
}