namespace BackCafeteria.Models
{
    public class HistorialCredito
    {

        public int Id { get; set; }
        public string NumeroControlAfectado { get; set; }  
        public decimal Cantidad { get; set; }
        public DateTime Fecha { get; set; }
        public string AutCorreo { get; set; } 

    }
}
