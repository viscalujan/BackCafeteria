namespace BackCafeteria.Models
{
    public class Venta
    {
        public int IdVentas { get; set; } // Antes Id
        public int FkIdUsuario { get; set; }
        public string MetodoPago { get; set; } = null!;
        public decimal TotalVenta { get; set; }
        public DateTime FechaVenta { get; set; }

        public virtual Usuario FkIdUsuarioNavigation { get; set; } = null!;
        public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();
    }
}
