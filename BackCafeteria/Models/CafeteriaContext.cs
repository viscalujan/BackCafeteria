using BackCafeteria.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeteriaAPI.Models
{
    public class CafeteriaContext : DbContext
    {
        public CafeteriaContext(DbContextOptions<CafeteriaContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();

        public DbSet<Aut> Aut { get; set; }

        public DbSet<HistorialCredito> HistorialCreditos => Set<HistorialCredito>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Usuario)
                .WithMany()
                .HasForeignKey(v => v.UsuarioId);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(d => d.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(d => d.VentaId);

            modelBuilder.Entity<HistorialCredito>(entity =>
            {
                entity.ToTable("HistorialCredito"); // <--- Esta línea corrige el nombre de la tabla
                entity.Property(h => h.NumeroControlAfectado).IsRequired().HasMaxLength(100);
                entity.Property(h => h.Cantidad).HasColumnType("decimal(10,2)");
                entity.Property(h => h.Fecha).IsRequired();
                entity.Property(h => h.AutCorreo).IsRequired().HasMaxLength(100);
            });


        }
    }
}
