using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("PasswordResets")]
    public class PasswordReset
    {
        [Key]
        [Column("id_reset")]
        public int IdReset { get; set; }

        [Column("correo")]
        public string Correo { get; set; } = null!;

        [Column("codigo")]
        public string Codigo { get; set; } = null!;

        [Column("expiracion")]
        public DateTime Expiracion { get; set; }
    }
}
