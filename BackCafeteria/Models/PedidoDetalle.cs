using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("PedidoDetalles")]
    public partial class PedidoDetalle
    {
        [Key]
        [Column("id_pdetalles")]
        public int IdPdetalles { get; set; }

        [Column("FK_id_pedidos")]
        public int FkIdPedidos { get; set; }

        [Column("FK_id_producto")]
        public int FkIdProducto { get; set; }

        [Column("cantidad_pdetalles")]
        public int CantidadPdetalles { get; set; }

        [Column("precio_detalles")]
        public decimal PrecioDetalles { get; set; }

        [ForeignKey("FkIdPedidos")]
        public virtual Pedido FkIdPedidosNavigation { get; set; } = null!;

        [ForeignKey("FkIdProducto")]
        public virtual Producto FkIdProductoNavigation { get; set; } = null!;
    }
}