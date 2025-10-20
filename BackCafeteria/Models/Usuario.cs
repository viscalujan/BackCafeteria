using System;
using System.Collections.Generic;

namespace BackCafeteria.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string CorreoUsuario { get; set; } = null!;

    public string? NumeroControl { get; set; }

    public decimal? Credito { get; set; }

    public string? Huella { get; set; }

    public string? RolUsuario { get; set; }

    public string? Codigqrtexto { get; set; }

    public string ContraUsuario { get; set; } = null!;

    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}
