using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace BackCafeteria.Models;

public partial class CafeteriaDbv2Context : DbContext
{
    public CafeteriaDbv2Context()
    {
    }

    public CafeteriaDbv2Context(DbContextOptions<CafeteriaDbv2Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Aut> Auts { get; set; }

    public virtual DbSet<EstadoPedido> EstadoPedidos { get; set; }

    public virtual DbSet<HistorialCredito> HistorialCreditos { get; set; }

    public virtual DbSet<Pedido> Pedidos { get; set; }

    public virtual DbSet<PedidoDetalle> PedidoDetalles { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Venta> Ventas { get; set; }

    public virtual DbSet<VentaDetalle> VentaDetalles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=cafeteriamysql.cjaak2yumuoz.us-east-2.rds.amazonaws.com;port=3306;database=CafeteriaDBv2;user=admin;password=Admon2025.", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.4.6-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Aut>(entity =>
        {
            entity.HasKey(e => e.IdAut).HasName("PRIMARY");

            entity.ToTable("Aut");

            entity.HasIndex(e => e.CorreoAut, "correo_aut").IsUnique();

            entity.Property(e => e.IdAut).HasColumnName("id_aut");
            entity.Property(e => e.ContraAut)
                .HasMaxLength(255)
                .HasColumnName("contra_aut");
            entity.Property(e => e.CorreoAut)
                .HasMaxLength(100)
                .HasColumnName("correo_aut");
            entity.Property(e => e.NombreAut)
                .HasMaxLength(100)
                .HasColumnName("nombre_aut");
            entity.Property(e => e.RolAut)
                .HasMaxLength(50)
                .HasColumnName("rol_aut");
        });

        modelBuilder.Entity<EstadoPedido>(entity =>
        {
            entity.HasKey(e => e.IdEstado).HasName("PRIMARY");

            entity.HasIndex(e => e.NombrePedido, "nombre_pedido").IsUnique();

            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.NombrePedido)
                .HasMaxLength(50)
                .HasColumnName("nombre_pedido");
        });

        modelBuilder.Entity<HistorialCredito>(entity =>
        {
            entity.HasKey(e => e.IdHcredito).HasName("PRIMARY");

            entity.ToTable("HistorialCredito");

            entity.Property(e => e.IdHcredito).HasColumnName("id_hcredito");
            entity.Property(e => e.AutCorreo).HasMaxLength(100);
            entity.Property(e => e.CantidadHcredito)
                .HasPrecision(10, 2)
                .HasColumnName("cantidad_hcredito");
            entity.Property(e => e.FechaHcredito)
                .HasColumnType("datetime")
                .HasColumnName("fecha_hcredito");
            entity.Property(e => e.Ncafectado)
                .HasMaxLength(20)
                .HasColumnName("ncafectado");
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.IdPedidos).HasName("PRIMARY");

            entity.HasIndex(e => e.FkIdEstado, "FK_pedidos_estado");

            entity.HasIndex(e => e.FkIdUsuario, "FK_pedidos_usuarios");

            entity.Property(e => e.IdPedidos).HasColumnName("id_pedidos");
            entity.Property(e => e.FechaPedido)
                .HasColumnType("datetime")
                .HasColumnName("fecha_pedido");
            entity.Property(e => e.FkIdEstado).HasColumnName("FK_id_estado");
            entity.Property(e => e.FkIdUsuario).HasColumnName("FK_id_usuario");
            entity.Property(e => e.TotalPedido)
                .HasPrecision(10, 2)
                .HasColumnName("total_pedido");

            entity.HasOne(d => d.FkIdEstadoNavigation).WithMany(p => p.Pedidos)
                .HasForeignKey(d => d.FkIdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pedidos_estado");

            entity.HasOne(d => d.FkIdUsuarioNavigation).WithMany(p => p.Pedidos)
                .HasForeignKey(d => d.FkIdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pedidos_usuarios");
        });

        modelBuilder.Entity<PedidoDetalle>(entity =>
        {
            entity.HasKey(e => e.IdPdetalles).HasName("PRIMARY");

            entity.HasIndex(e => e.FkIdPedidos, "FK_pdetalles_pedidos");

            entity.HasIndex(e => e.FkIdProducto, "FK_pdetalles_productos");

            entity.Property(e => e.IdPdetalles).HasColumnName("id_pdetalles");
            entity.Property(e => e.CantidadPdetalles).HasColumnName("cantidad_pdetalles");
            entity.Property(e => e.FkIdPedidos).HasColumnName("FK_id_pedidos");
            entity.Property(e => e.FkIdProducto).HasColumnName("FK_id_producto");
            entity.Property(e => e.PrecioDetalles)
                .HasPrecision(10, 2)
                .HasColumnName("precio_detalles");

            entity.HasOne(d => d.FkIdPedidosNavigation).WithMany(p => p.PedidoDetalles)
                .HasForeignKey(d => d.FkIdPedidos)
                .HasConstraintName("FK_pdetalles_pedidos");

            entity.HasOne(d => d.FkIdProductoNavigation).WithMany(p => p.PedidoDetalles)
                .HasForeignKey(d => d.FkIdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pdetalles_productos");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PRIMARY");

            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.CantidadProducto).HasColumnName("cantidad_producto");
            entity.Property(e => e.NombreProducto)
                .HasMaxLength(100)
                .HasColumnName("nombre_producto");
            entity.Property(e => e.PrecioProducto)
                .HasPrecision(10, 2)
                .HasColumnName("precio_producto");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PRIMARY");

            entity.HasIndex(e => e.CorreoUsuario, "correo_usuario").IsUnique();

            entity.HasIndex(e => e.NumeroControl, "numero_control").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Codigqrtexto)
                .HasMaxLength(255)
                .HasColumnName("codigqrtexto");
            entity.Property(e => e.ContraUsuario)
                .HasMaxLength(255)
                .HasColumnName("contra_usuario");
            entity.Property(e => e.CorreoUsuario)
                .HasMaxLength(100)
                .HasColumnName("correo_usuario");
            entity.Property(e => e.Credito)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("credito");
            entity.Property(e => e.Huella)
                .HasMaxLength(50)
                .HasColumnName("huella");
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(100)
                .HasColumnName("nombre_usuario");
            entity.Property(e => e.NumeroControl)
                .HasMaxLength(20)
                .HasColumnName("numero_control");
            entity.Property(e => e.RolUsuario)
                .HasMaxLength(50)
                .HasColumnName("rol_usuario");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.IdVentas).HasName("PRIMARY");

            entity.HasIndex(e => e.FkIdUsuario, "FK_ventas_usuarios");

            entity.Property(e => e.IdVentas).HasColumnName("id_ventas");
            entity.Property(e => e.FechaVenta)
                .HasColumnType("datetime")
                .HasColumnName("fecha_venta");
            entity.Property(e => e.FkIdUsuario).HasColumnName("FK_id_usuario");
            entity.Property(e => e.MetodoPago)
                .HasMaxLength(50)
                .HasColumnName("metodo_pago");
            entity.Property(e => e.TotalVenta)
                .HasPrecision(10, 2)
                .HasColumnName("total_venta");

            entity.HasOne(d => d.FkIdUsuarioNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.FkIdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ventas_usuarios");
        });

        modelBuilder.Entity<VentaDetalle>(entity =>
        {
            entity.HasKey(e => e.IdVdetalle).HasName("PRIMARY");

            entity.HasIndex(e => e.FkIdProducto, "FK_vdetalle_productos");

            entity.HasIndex(e => e.FkIdVenta, "FK_vdetalle_ventas");

            entity.Property(e => e.IdVdetalle).HasColumnName("id_vdetalle");
            entity.Property(e => e.CantidadProducto).HasColumnName("cantidad_producto");
            entity.Property(e => e.FkIdProducto).HasColumnName("FK_id_producto");
            entity.Property(e => e.FkIdVenta).HasColumnName("FK_id_venta");
            entity.Property(e => e.PrecioUnitario)
                .HasPrecision(10, 2)
                .HasColumnName("precio_unitario");

            entity.HasOne(d => d.FkIdProductoNavigation).WithMany(p => p.VentaDetalles)
                .HasForeignKey(d => d.FkIdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_vdetalle_productos");

            entity.HasOne(d => d.FkIdVentaNavigation).WithMany(p => p.VentaDetalles)
                .HasForeignKey(d => d.FkIdVenta)
                .HasConstraintName("FK_vdetalle_ventas");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
