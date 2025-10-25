namespace BackCafeteria.Models
{
    public class HistorialCredito
    {
        public int IdHistorialCredito { get; set; } // ✅ clave primaria
        public int FkIdUsuario { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaMovimiento { get; set; }

        public Usuario? FkIdUsuarioNavigation { get; set; }
    }
}
