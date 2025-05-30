namespace CafeteriaAPI.DTOs
{
    public class UsuarioCreateDTO
    {
        public string Nombre { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string NumeroControl { get; set; } = null!;
    }
}
