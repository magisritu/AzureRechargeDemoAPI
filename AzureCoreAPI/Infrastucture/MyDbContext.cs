using AzureCoreAPI.Entity;
using Microsoft.EntityFrameworkCore;

namespace AzureCoreAPI.Infrastucture
{
    public class MyDbContext: DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
        {
        }

        // Define DbSet properties for your entities
        public DbSet<Weather> Weathers { get; set; }
        public DbSet<UserDetails> UserDetails { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Weather>().ToTable("Weather");
            modelBuilder.Entity<UserDetails>().ToTable("UserDetails");
        }
    }
}
