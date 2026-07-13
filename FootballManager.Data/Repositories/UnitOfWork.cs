using FootballManager.Data.Repositories.Interfaces;
using System.Collections;

namespace FootballManager.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FootballContext _context;
        private Hashtable _repositories;

        public UnitOfWork(FootballContext context)
        {
            _context = context;
            _repositories = new Hashtable();
        }

        public IBaseRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(BaseRepository<>);
                var repositoryInstance = Activator.CreateInstance(
                    repositoryType.MakeGenericType(typeof(T)),
                    _context
                );

                _repositories.Add(type, repositoryInstance);
            }

            return (IBaseRepository<T>)_repositories[type]!;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}