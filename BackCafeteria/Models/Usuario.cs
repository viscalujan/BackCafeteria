using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("nombre_usuario")]
        public string? NombreUsuario { get; set; }

        [Column("correo_usuario")]
        public string? CorreoUsuario { get; set; }

        [Column("contra_usuario")]
        public string? ContraUsuario { get; set; }

        [Column("numero_control")]
        public string? NumeroControl { get; set; }

        [Column("rol_usuario")]
        public string? RolUsuario { get; set; }

        [Column("huella")]
        public byte[]? Huella { get; set; }

        [NotMapped]
        public string? HuellaBase64
        {
            get => Huella != null ? Convert.ToBase64String(Huella) : null;
            set => Huella = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
        }

        [Column("credito")]
        public decimal Credito { get; set; }

        [Column("codigqrtexto")]
        public string? Codigqrtexto { get; set; }

        public virtual ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }

    [Table("Aut")]
    public class Aut
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
