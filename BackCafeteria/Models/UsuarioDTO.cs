namespace CafeteriaAPI.Models
{
    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string NumeroControl { get; set; }
        public decimal Credito { get; set; }

    }
}
