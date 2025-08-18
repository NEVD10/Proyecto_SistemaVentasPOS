// Data/DBContexto.cs
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Models;

namespace SistemaVentas.Data
{
    public class DBContexto : DbContext
    {
        public DBContexto(DbContextOptions<DBContexto> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        // Agregar otros DbSet para las demás entidades cuando se implementen

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("Cliente");
                entity.HasKey(e => e.IdCliente);
                entity.Property(e => e.TipoDocumento).IsRequired().HasMaxLength(15);
                entity.Property(e => e.NumeroDocumento).IsRequired().HasMaxLength(12);
                entity.Property(e => e.Nombres).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Apellidos).HasMaxLength(60);
                entity.Property(e => e.Telefono).HasMaxLength(9);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Direccion).HasMaxLength(100);
                entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()");
            });

            // Configuraciones para otras entidades se agregarán después
        }
    }
}