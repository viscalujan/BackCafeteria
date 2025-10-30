using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("HistorialCredito")]
    public class HistorialCredito
    {
        [Key]
        [Column("id_hcredito")]
        public int IdHistorialCredito { get; set; }

        [Column("ncafectado")]
        public string? NumeroControlAfectado { get; set; }

        [Column("cantidad_hcredito")]
        public decimal Monto { get; set; }

        [Column("fecha_hcredito")]
        public DateTime FechaMovimiento { get; set; }

        [Column("AutCorreo")]
        public string? AutCorreo { get; set; }

        // Si quieres mantener la relación, usa el mismo tipo que la clave primaria de Usuario
        [Column("FK_id_usuario")]
        public int? FkIdUsuario { get; set; }

        [ForeignKey("FkIdUsuario")]
        public virtual Usuario? FkIdUsuarioNavigation { get; set; }
    }
}