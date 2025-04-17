using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlogApp.DAL.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(object id);
        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        // Add a single entity
        Task AddAsync(TEntity entity);

        // Add multiple entities
        Task AddRangeAsync(IEnumerable<TEntity> entities);

        // Remove a single entity (Mark for deletion)
        void Remove(TEntity entity);

        // Remove multiple entities (Mark for deletion)
        void RemoveRange(IEnumerable<TEntity> entities);
    }
}