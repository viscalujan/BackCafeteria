using System.ComponentModel.DataAnnotations; // 🔹 Necesario para [Required]

namespace BackCafeteria.DTOs
{
    public class ProductoCreateDTO
    {
        [Required]
        public string Nombre { get; set; } = null!;

        [Required]
        public string NombreProducto { get; set; } = null!;

        public decimal Precio { get; set; }

        public int CantidadProducto { get; set; }
    }
}
