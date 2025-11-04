using System;
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

        // 🔹 Eliminamos la columna para EF (no existe en DB)
        [NotMapped]
        public int? FkIdUsuario { get; set; }

        [NotMapped]
        public virtual Usuario? FkIdUsuarioNavigation { get; set; }

        [NotMapped]
        public decimal Cantidad
        {
            get => Monto;
            set => Monto = value;
        }

        [NotMapped]
        public DateTime Fecha
        {
            get => FechaMovimiento;
            set => FechaMovimiento = value;
        }
    }
}
