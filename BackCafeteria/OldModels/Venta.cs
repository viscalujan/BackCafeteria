using CafeteriaAPI.Models;

public class Venta
{
    public int Id { get; set; }

    public int? UsuarioId { get; set; } 

    public DateTime Fecha { get; set; } = DateTime.Now;

    public decimal Total { get; set; }

    public string MetodoPago { get; set; } = null!;

    public Usuario? Usuario { get; set; }

    public List<VentaDetalle> Detalles { get; set; } = new();

  

}
