using BackCafeteria.Models;
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

        // 🔹 Eliminadas FK y navegación para Usuario
        // public int? UsuarioId { get; set; }
        // public virtual Usuario? Usuario { get; set; }

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
