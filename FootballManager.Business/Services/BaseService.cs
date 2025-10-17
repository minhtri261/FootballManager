using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Repositories.Interfaces;

namespace FootballManager.Business.Services
{
    public class BaseService<T> : IBaseService<T> where T : class
    {
        protected readonly IGenericRepository<T> _repository;

        public BaseService(IGenericRepository<T> repository)
        {
            _repository = repository;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync() => await _repository.GetAllAsync();

        public virtual async Task<T?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);

        public virtual async Task AddAsync(T entity)
        {
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _repository.Update(entity);
            await _repository.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(T entity)
        {
            _repository.Delete(entity);
            await _repository.SaveChangesAsync();
        }
    }
}
