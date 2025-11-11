namespace BackCafeteria.DTOs
{
    public class PedidoResponseDTO
    {

        public int IdPedido { get; set; }
        public string Estado { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public List<PedidoDetalleResponseDTO> Detalles { get; set; } = new();

    }
}
