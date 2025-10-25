using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("Auth")]
    public class Auth
    {
        [Key]
        [Column("id_aut")]
        public int IdAut { get; set; }

        [Column("nombre_aut")]
        public string NombreAut { get; set; } = null!;

        [Column("correo_aut")]
        public string CorreoAut { get; set; } = null!;

        [Column("contra_aut")]
        public string ContraAut { get; set; } = null!;

        [Column("rol_aut")]
        public string? RolAut { get; set; }
    }
}
