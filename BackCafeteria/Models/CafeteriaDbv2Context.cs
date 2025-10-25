using Microsoft.EntityFrameworkCore;

namespace BackCafeteria.Models
{
    public class CafeteriaDbv2Context : DbContext
    {
        public CafeteriaDbv2Context(DbContextOptions<CafeteriaDbv2Context> options)
            : base(options)
        {
        }
        public DbSet<Aut> Aut { get; set; } = null!;

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<Venta> Ventas { get; set; } = null!;
        public DbSet<VentaDetalle> VentaDetalles { get; set; } = null!;
        public DbSet<HistorialCredito>? HistorialCreditos { get; set; } // Asegúrate de tener una clase HistorialCredito

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Aut>().HasKey(a => a.IdAut);


            // Claves primarias
            modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);
            modelBuilder.Entity<Producto>().HasKey(p => p.Id);
            modelBuilder.Entity<Venta>().HasKey(v => v.IdVentas);
            modelBuilder.Entity<VentaDetalle>().HasKey(vd => vd.IdVdetalle);

            // 🔹 Aquí agregamos la clave primaria para evitar el error
            modelBuilder.Entity<HistorialCredito>().HasKey(h => h.IdHistorialCredito);

            // Relaciones
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.FkIdUsuarioNavigation)
                .WithMany(u => u.Ventas)
                .HasForeignKey(v => v.FkIdUsuario);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(vd => vd.FkIdVentaNavigation)
                .WithMany(v => v.VentaDetalles)
                .HasForeignKey(vd => vd.FkIdVenta);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(vd => vd.FkIdProductoNavigation)
                .WithMany(p => p.VentaDetalles)
                .HasForeignKey(vd => vd.FkIdProducto);
        }
    }
}
