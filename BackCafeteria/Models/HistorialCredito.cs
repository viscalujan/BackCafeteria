using System;
using System.Collections.Generic;

namespace BackCafeteria.Models;

public partial class HistorialCredito
{
    public int IdHcredito { get; set; }

    public string? Ncafectado { get; set; }

    public decimal CantidadHcredito { get; set; }

    public DateTime FechaHcredito { get; set; }

    public string? AutCorreo { get; set; }
}
