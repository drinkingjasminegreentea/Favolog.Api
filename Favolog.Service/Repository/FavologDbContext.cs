using Favolog.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace Favolog.Service.Repository
{
    public class FavologDbContext: DbContext
    {
        public FavologDbContext(DbContextOptions<FavologDbContext> options): base(options) { }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Item>().ToTable("Item", "dbo");            
            modelBuilder.Entity<Catalog>().ToTable("Catalog", "dbo");            
            modelBuilder.Entity<User>().ToTable("User","dbo");
            modelBuilder.Entity<UserFollow>().ToTable("UserFollow", "dbo");
            modelBuilder.Entity<UserFeedItem>().ToTable("vw_UserFeed", "dbo");            
        }
    }
}
