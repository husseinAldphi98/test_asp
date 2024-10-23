using System.Linq.Expressions;

namespace UserSystem.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
        Task<T> GetAsync(Expression<Func<T, bool>> filter = null, bool tracked = true);
        Task CreateAsync(T entiry);
        Task RemoveAsync(T entiry);
        Task SaveAsync();
    }
}
