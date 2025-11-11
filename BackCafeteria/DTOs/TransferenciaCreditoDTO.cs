namespace BackCafeteria.DTOs
{
    public class TransferenciaCreditoDTO
    {

        public string NumeroControlEmisor { get; set; } = null!;
        public string NumeroControlReceptor { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public string ContrasenaEmisor { get; set; } = null!;

    }
}
