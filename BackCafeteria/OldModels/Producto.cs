using System.ComponentModel.DataAnnotations;

namespace CafeteriaAPI.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } = null!;

        [Required]
        public decimal Precio { get; set; }

        [Required]
        public int Cantidad { get; set; }  // <-- antes era Stock
    }
}
