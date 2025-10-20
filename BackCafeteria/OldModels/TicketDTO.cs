namespace BackCafeteria.Models
{
    public class TicketDTO
    {

        public int Id { get; set; }
        public decimal Total { get; set; }
        public DateTime Fecha { get; set; }
        public string Metodo { get; set; } = "";

    }
}
