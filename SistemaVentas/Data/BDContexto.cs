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
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Cliente
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("Cliente");
                entity.HasKey(e => e.IdCliente);
                entity.Property(e => e.TipoDocumento).IsRequired().HasMaxLength(20);
                entity.Property(e => e.NumeroDocumento).IsRequired().HasMaxLength(12).IsUnicode(false);
                entity.Property(e => e.Nombres).IsRequired().HasMaxLength(30).IsUnicode(false);
                entity.Property(e => e.Apellidos).HasMaxLength(60).IsUnicode(false);
                entity.Property(e => e.Telefono).HasMaxLength(9).IsUnicode(false);
                entity.Property(e => e.Email).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.Direccion).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()");
            });

            // Configuración de Producto
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.ToTable("Producto");
                entity.HasKey(e => e.IdProducto);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.CodigoBarras).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.PrecioCosto).HasColumnType("decimal(10, 2)").IsRequired();
                entity.Property(e => e.PrecioVenta).HasColumnType("decimal(10, 2)").IsRequired();
                entity.Property(e => e.Stock).IsRequired();
                entity.Property(e => e.Marca).HasMaxLength(30).IsUnicode(false);
                entity.Property(e => e.IdCategoria).IsRequired();
                entity.Property(e => e.Estado).HasDefaultValue(true);

                entity.HasOne(p => p.Categoria)
                      .WithMany()
                      .HasForeignKey(p => p.IdCategoria)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("Categoria");
                entity.HasKey(e => e.IdCategoria);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(30).IsUnicode(false);
            });

            // Configuración de Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");
                entity.HasKey(e => e.IdUsuario);
                entity.Property(e => e.NombreUsuario).IsRequired().HasMaxLength(20).IsUnicode(false);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255).IsUnicode(false);
                entity.Property(e => e.NombreCompleto).IsRequired().HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.Rol).IsRequired().HasMaxLength(15).IsUnicode(false);
                entity.Property(e => e.Estado).HasDefaultValue(true);
            });

            // Configuración de Venta
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.ToTable("Venta");
                entity.HasKey(e => e.IdVenta);
                entity.Property(e => e.FechaVenta).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.TipoComprobante).IsRequired().HasMaxLength(20).IsUnicode(false);
                entity.Property(e => e.NumeroComprobante).HasMaxLength(20).IsUnicode(false);
                entity.Property(e => e.Subtotal).HasColumnType("decimal(10, 2)").IsRequired();
                entity.Property(e => e.MontoIGV).HasColumnType("decimal(10, 2)").IsRequired().HasDefaultValue(0);
                entity.Property(e => e.MontoTotal).HasColumnType("decimal(10, 2)").IsRequired();
                entity.Property(e => e.MetodoPago).IsRequired().HasMaxLength(20).IsUnicode(false).HasDefaultValue("Efectivo");

                entity.HasOne(v => v.Cliente)
                      .WithMany()
                      .HasForeignKey(v => v.IdCliente)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Usuario)
                      .WithMany()
                      .HasForeignKey(v => v.IdUsuario)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de DetalleVenta
            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.ToTable("DetalleVenta");
                entity.HasKey(e => e.IdDetalleVenta);
                entity.Property(e => e.Cantidad).IsRequired();
                entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(10, 2)").IsRequired();
                entity.Property(e => e.SubtotalLinea).HasColumnType("decimal(10, 2)").IsRequired();

                entity.HasOne(d => d.Venta)
                      .WithMany(v => v.DetalleVentas) // Ahora usa la propiedad añadida
                      .HasForeignKey(d => d.IdVenta)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Producto)
                      .WithMany()
                      .HasForeignKey(d => d.IdProducto)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}