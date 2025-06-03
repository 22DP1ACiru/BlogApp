using System.Linq.Expressions;

namespace BlogApp.DAL.Interfaces
{
    /// <summary>
    /// Defines a generic repository pattern for data access operations.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity this repository handles.</typeparam>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Retrieves an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key of the entity.</param>
        /// <returns>The entity if found; otherwise, null.</returns>
        Task<TEntity?> GetByIdAsync(object id);

        /// <summary>
        /// Retrieves all entities of type TEntity.
        /// </summary>
        /// <remarks>
        /// Caution: Using this method for tables with a large number of records can impact performance.
        /// Consider using specific find methods or pagination for large datasets.
        /// </remarks>
        Task<IEnumerable<TEntity>> GetAllAsync();

        /// <summary>
        /// Finds entities based on a specified predicate (filter condition).
        /// </summary>
        /// <param name="predicate">The expression to filter entities.</param>
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Adds a single entity to the data store.
        /// The entity is marked for addition and will be saved upon calling IUnitOfWork.CompleteAsync().
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        Task AddAsync(TEntity entity);

        /// <summary>
        /// Adds a collection of entities to the data store.
        /// The entities are marked for addition and will be saved upon calling IUnitOfWork.CompleteAsync().
        /// </summary>
        /// <param name="entities">The collection of entities to add.</param>
        Task AddRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// Marks a single entity for removal from the data store.
        /// The entity will be deleted upon calling IUnitOfWork.CompleteAsync().
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        void Remove(TEntity entity);

        /// <summary>
        /// Marks a collection of entities for removal from the data store.
        /// The entities will be deleted upon calling IUnitOfWork.CompleteAsync().
        /// </summary>
        /// <param name="entities">The collection of entities to remove.</param>
        void RemoveRange(IEnumerable<TEntity> entities);
    }
}