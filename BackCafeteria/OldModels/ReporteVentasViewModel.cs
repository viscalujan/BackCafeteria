namespace BackCafeteria.Models
{
    public class ReporteVentasViewModel
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public List<DetalleVentaView> Detalles { get; set; } = new();
        public List<ResumenProducto> ResumenProductos { get; set; } = new();
        public decimal TotalGeneral => Detalles.Sum(d => d.Total);
    }

    // Modelo para cada línea de detalle
    public class DetalleVentaView
    {
        public int VentaId { get; set; }
        public DateTime Fecha { get; set; }
        public string ProductoNombre { get; set; } // <- Nombre del producto
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total => Cantidad * PrecioUnitario;
        public string MetodoPago { get; set; }
    }

    // Modelo para el resumen por producto
    public class ResumenProducto
    {
        public string ProductoNombre { get; set; }
        public int UnidadesVendidas { get; set; }
        public decimal TotalVendido { get; set; }
    }
}
