using ExtractorFacturasAzure.Models;
using Microsoft.EntityFrameworkCore;

namespace ExtractorFacturasAzure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Factura> Facturas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuramos explícitamente la precisión del decimal para dinero
            modelBuilder.Entity<Factura>()
                .Property(f => f.TotalPagar)
                .HasPrecision(18, 2);
        }
    }
}