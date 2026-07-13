using FootballManager.Business.Services.Interfaces;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Business.Services
{
    public class BaseService<T> : IBaseService<T> where T : class
    {
        protected readonly IBaseRepository<T> _repository;
        protected readonly IUnitOfWork _unitOfWork;

        public BaseService(IBaseRepository<T> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync() => await _repository.GetAll().ToListAsync();

        public virtual async Task<T?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);

        public virtual async Task AddAsync(T entity)
        {
            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(T entity)
        {
            _repository.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
