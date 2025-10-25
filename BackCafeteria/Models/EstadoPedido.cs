using System;
using System.Collections.Generic;

namespace BackCafeteria.Models;

public partial class EstadoPedido
{
    public int IdEstado { get; set; }
    public string NombrePedido { get; set; } = null!;
    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
