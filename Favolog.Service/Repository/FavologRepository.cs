using Favolog.Service.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Linq;

namespace Favolog.Service.Repository
{
    public class FavologRepository: IFavologRepository
    {
        private FavologDbContext _dbContext;

        public FavologRepository(FavologDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<TEntity> Get<TEntity>(int? id = null) where TEntity : Entity
        {
            if (id.HasValue)
                return _dbContext.Set<TEntity>().Where(item => item.Id == id);

            return _dbContext.Set<TEntity>();
        }

        public void Attach<TEntity>(TEntity entity) where TEntity : Entity
        {
            _dbContext.Attach(entity);
        }

        public void SaveChanges()
        {
            _dbContext.SaveChanges();
        }

        public EntityEntry<TEntity> Delete<TEntity>(TEntity item) where TEntity : Entity
        {            
            return _dbContext.Remove(item);
        }

        public void Delete<TEntity>(IEnumerable<TEntity> items) where TEntity : Entity
        {            
            _dbContext.RemoveRange(items);
        }
    }
}
