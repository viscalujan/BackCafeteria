using System;
using System.Collections.Generic;

namespace BackCafeteria.Models;

public partial class Aut
{
    public int IdAut { get; set; }

    public string NombreAut { get; set; } = null!;

    public string CorreoAut { get; set; } = null!;

    public string ContraAut { get; set; } = null!;

    public string? RolAut { get; set; }
}
