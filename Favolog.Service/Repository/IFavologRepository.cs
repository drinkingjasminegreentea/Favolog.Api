using Favolog.Service.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Linq;

namespace Favolog.Service.Repository
{
    public interface IFavologRepository
    {
        IQueryable<TEntity> Get<TEntity>(int? id = null) where TEntity : Entity;

        void Attach<TEntity>(TEntity entity) where TEntity : Entity;

        EntityEntry<TEntity> Delete<TEntity>(TEntity item) where TEntity : Entity;

        void Delete<TEntity>(IEnumerable<TEntity> items) where TEntity : Entity;

        void SaveChanges();        
    }
}
