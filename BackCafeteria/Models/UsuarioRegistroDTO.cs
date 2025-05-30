using System.ComponentModel.DataAnnotations;

namespace CafeteriaAPI.Models
{
    public class UsuarioRegistroDTO
    {
        public string Nombre { get; set; }
        public string Correo { get; set; }

        public string NumeroControl { get; set; }

        [Range(50, double.MaxValue, ErrorMessage = "El crédito inicial debe ser mayor o igual a 50.")]
        public decimal Credito { get; set; }

        public string Contrasena { get; set; }
    }
}
