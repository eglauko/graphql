using Api.Core.Domain.Localizacoes;
using Api.Core.Domain.Lojas;
using Microsoft.EntityFrameworkCore;

namespace Api.Infra.Persistence
{
    public class AppDbContext : DbContext
    {
        private readonly string dbName;

        public AppDbContext()
        {
            dbName = nameof(AppDbContext);
        }

        public AppDbContext(string dbName)
        {
            this.dbName = dbName;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseInMemoryDatabase(dbName);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Empresa>().HasKey(e => e.Id);
            modelBuilder.Entity<Loja>().HasKey(l => l.Id);
            modelBuilder.Owned<Endereco>();
        }
    }
}
