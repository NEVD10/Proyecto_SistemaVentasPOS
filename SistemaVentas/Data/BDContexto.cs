using Microsoft.EntityFrameworkCore;
using SistemaVentas.Models;

namespace SistemaVentas.Data
{
    public class DBContexto : DbContext
    {
        public DBContexto(DbContextOptions<DBContexto> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }

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

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.ToTable("Producto");
                entity.HasKey(e => e.IdProducto);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CodigoBarras).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PrecioCosto).HasColumnType("decimal(10, 2)").IsRequired();
                entity.Property(e => e.PrecioVenta).HasColumnType("decimal(10, 2)").IsRequired();
                entity.Property(e => e.Stock).IsRequired();
                entity.Property(e => e.Marca).HasMaxLength(30);
                entity.Property(e => e.IdCategoria).IsRequired(); // Aseguramos que IdCategoria sea obligatorio
                entity.Property(e => e.Estado).HasDefaultValue(true);

                entity.HasOne(p => p.Categoria)
                      .WithMany()
                      .HasForeignKey(p => p.IdCategoria)
                      .IsRequired() // Indicamos que la relación es obligatoria
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("Categoria");
                entity.HasKey(e => e.IdCategoria);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(30);
            });
        }
    }
}