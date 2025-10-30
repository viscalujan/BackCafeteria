using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("VentaDetalles")]
    public class VentaDetalle
    {
        [Key]
        [Column("id_vdetalle")]
        public int IdVdetalle { get; set; }

        [Column("FK_id_venta")]
        public int FkIdVenta { get; set; }

        [Column("FK_id_producto")]
        public int FkIdProducto { get; set; }

        [Column("cantidad_producto")]
        public int CantidadProducto { get; set; }

        [Column("precio_unitario")]
        public decimal PrecioUnitario { get; set; }

        [ForeignKey("FkIdVenta")]
        public virtual Venta FkIdVentaNavigation { get; set; } = null!;

        [ForeignKey("FkIdProducto")]
        public virtual Producto FkIdProductoNavigation { get; set; } = null!;
    }
}