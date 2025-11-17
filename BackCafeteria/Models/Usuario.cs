using System.Collections.Generic;
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
        public string NombreUsuario { get; set; } = null!;

        [Column("correo_usuario")]
        public string CorreoUsuario { get; set; } = null!;

        [Column("numero_control")]
        public string? NumeroControl { get; set; }

        [Column("credito")]
        public decimal Credito { get; set; }

        [Column("huella")]
        public string? Huella { get; set; }

        [Column("rol_usuario")]
        public string? RolUsuario { get; set; }

        [Column("codigqrtexto")]
        public string? CodigoQRTexto { get; set; }

        [Column("contra_usuario")]
        public string ContraUsuario { get; set; } = null!;

        public int IniciosSesion { get; set; } = 0;


        // 🔹 Simulación de HuellaBase64
        [NotMapped]
        public string? HuellaBase64
        {
            get => Huella;
            set => Huella = value;
        }

        // 🔹 Navegación a ventas y pedidos
        public virtual ICollection<Venta>? Ventas { get; set; }
        public virtual ICollection<Pedido>? Pedidos { get; set; }

        // 🔹 Eliminada navegación a HistorialCredito porque la tabla no tiene FK
        // public virtual ICollection<HistorialCredito>? HistorialCreditos { get; set; }
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
