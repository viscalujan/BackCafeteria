using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("Productos")]
    public class Producto
    {
        [Key]
        [Column("id_producto")]
        public int Id { get; set; }

        [Column("nombre_producto")]
        public string Nombre { get; set; } = null!;

        [Column("precio_producto")]
        public decimal Precio { get; set; }

        [Column("cantidad_producto")]
        public int CantidadProducto { get; set; }  // Nueva propiedad agregada

        public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();
        public virtual ICollection<PedidoDetalle> PedidoDetalles { get; set; } = new List<PedidoDetalle>();  // Agregado para Pedidos
    }
}