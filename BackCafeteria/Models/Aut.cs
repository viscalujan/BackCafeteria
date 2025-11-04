using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Auth")]
public class Auth
{
    [Key]
    [Column("id_aut")]
    public int IdAut { get; set; }

    [Required]
    [Column("nombre_aut")]
    public string NombreAut { get; set; } = null!;

    [Required]
    [EmailAddress] // opcional
    [Column("correo_aut")]
    public string CorreoAut { get; set; } = null!;

    [Required]
    [Column("contra_aut")]
    public string ContraAut { get; set; } = null!;

    [Column("rol_aut")]
    public string? RolAut { get; set; }
}
