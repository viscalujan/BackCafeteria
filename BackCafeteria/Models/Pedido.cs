using CafeteriaAPI.Models;
using System;
using System.Collections.Generic;

namespace BackCafeteria.Models;

public partial class Pedido
{
    public int IdPedidos { get; set; }
    public int FkIdUsuario { get; set; }
    public DateTime FechaPedido { get; set; }
    public decimal? TotalPedido { get; set; }
    public int FkIdEstado { get; set; }
    public virtual EstadoPedido FkIdEstadoNavigation { get; set; } = null!;
    public virtual Usuario FkIdUsuarioNavigation { get; set; } = null!;
    public virtual ICollection<PedidoDetalle> PedidoDetalles { get; set; } = new List<PedidoDetalle>();
}
