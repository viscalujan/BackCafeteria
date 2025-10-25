namespace BackCafeteria.Models
{
    public class VentaDetalle
    {
        public int IdVdetalle { get; set; }
        public int FkIdVenta { get; set; } // Antes VentaId
        public int FkIdProducto { get; set; }
        public int CantidadProducto { get; set; }
        public decimal PrecioUnitario { get; set; }

        public virtual Venta FkIdVentaNavigation { get; set; } = null!;
        public virtual Producto FkIdProductoNavigation { get; set; } = null!;
    }
}
