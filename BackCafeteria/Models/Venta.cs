using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("Ventas")]
    public class Venta
    {
        [Key]
        [Column("id_ventas")]
        public int IdVentas { get; set; }

        [Column("FK_id_usuario")]
        public int FkIdUsuario { get; set; }

        [Column("fecha_venta")]
        public DateTime FechaVenta { get; set; }

        [Column("total_venta")]
        public decimal TotalVenta { get; set; }

        [Column("metodo_pago")]
        public string MetodoPago { get; set; } = null!;

        [ForeignKey("FkIdUsuario")]
        public virtual Usuario FkIdUsuarioNavigation { get; set; } = null!;

        public int? FkIdPedido { get; set; }

        public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();

        // 🔹 Alias para compatibilidad con el controlador
        [NotMapped]
        public decimal Total
        {
            get => TotalVenta;
            set => TotalVenta = value;
        }


    }
}
