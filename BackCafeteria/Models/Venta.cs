using System;
using System.Collections.Generic;

namespace BackCafeteria.Models;

public partial class Venta
{
    public int IdVentas { get; set; }

    public int FkIdUsuario { get; set; }

    public DateTime FechaVenta { get; set; }

    public decimal TotalVenta { get; set; }

    public string? MetodoPago { get; set; }

    public virtual Usuario FkIdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();
}
