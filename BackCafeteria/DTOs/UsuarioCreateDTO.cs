using System.ComponentModel.DataAnnotations;

public class UsuarioCreateDTO
{
    public int Id { get; set; }
    [Required]
    public string Nombre { get; set; } = null!;
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = null!;
    [Required]
    public string NumeroControl { get; set; } = null!;
    [Required]
    public string Contra { get; set; } = null!;
    public decimal Credito { get; set; }
    public string? CodigoQRTexto { get; set; }

    // 🔥 AGREGAR DE VUELTA la propiedad Rol
    public string Rol { get; set; } = "alumno"; // ✅ Valor por defecto
}


public class AumentoCreditoDTO
    {
        [Required(ErrorMessage = "El número de control es obligatorio.")]
        public string NumeroControl { get; set; } = null!;

        [Range(50, double.MaxValue, ErrorMessage = "La cantidad debe ser al menos 50.")]
        public decimal Cantidad { get; set; }
    }
