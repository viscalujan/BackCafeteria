using BackCafeteria.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

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
        public virtual DbSet<HistorialCredito> HistorialCreditos { get; set; }


        // Si tienes pedidos y estados:
        public DbSet<Pedido> Pedidos { get; set; } = null!;
        public DbSet<PedidoDetalle> PedidoDetalles { get; set; } = null!;
        public DbSet<EstadoPedido> EstadosPedido { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Claves
            modelBuilder.Entity<Aut>().HasKey(a => a.IdAut);
            modelBuilder.Entity<Usuario>().HasIndex(u => u.NumeroControl).IsUnique();
            modelBuilder.Entity<Producto>().HasKey(p => p.Id);
            modelBuilder.Entity<Venta>().HasKey(v => v.IdVentas);
            modelBuilder.Entity<VentaDetalle>().HasKey(vd => vd.IdVdetalle);
            modelBuilder.Entity<Pedido>().HasKey(p => p.IdPedidos);
            modelBuilder.Entity<PedidoDetalle>().HasKey(pd => pd.IdPdetalles);
            modelBuilder.Entity<EstadoPedido>().HasKey(ep => ep.IdEstado);
            modelBuilder.Entity<HistorialCredito>().HasKey(h => h.IdHistorialCredito);

            // Relaciones
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.FkIdUsuarioNavigation)
                .WithMany(u => u.Ventas)
                .HasForeignKey(v => v.FkIdUsuario);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.FkIdUsuarioNavigation)
                .WithMany(u => u.Pedidos)
                .HasForeignKey(p => p.FkIdUsuario);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.FkIdEstadoNavigation)
                .WithMany(ep => ep.Pedidos)
                .HasForeignKey(p => p.FkIdEstado);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(vd => vd.FkIdVentaNavigation)
                .WithMany(v => v.VentaDetalles)
                .HasForeignKey(vd => vd.FkIdVenta);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(vd => vd.FkIdProductoNavigation)
                .WithMany(p => p.VentaDetalles)
                .HasForeignKey(vd => vd.FkIdProducto);

            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(pd => pd.FkIdPedidosNavigation)
                .WithMany(p => p.PedidoDetalles)
                .HasForeignKey(pd => pd.FkIdPedidos);

            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(pd => pd.FkIdProductoNavigation)
                .WithMany(p => p.PedidoDetalles)
                .HasForeignKey(pd => pd.FkIdProducto);

            // 🔹 Relación HistorialCredito (basada en tu modelo actual)
            modelBuilder.Entity<HistorialCredito>()
                .HasOne(h => h.FkIdUsuarioNavigation)
                .WithMany(u => u.HistorialCreditos)
                .HasForeignKey(h => h.FkIdUsuario);


        }
    }
}
