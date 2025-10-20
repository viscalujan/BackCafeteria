using System;
using System.Collections.Generic;

namespace BackCafeteria.Models;

public partial class PedidoDetalle
{
    public int IdPdetalles { get; set; }

    public int FkIdPedidos { get; set; }

    public int FkIdProducto { get; set; }

    public int CantidadPdetalles { get; set; }

    public decimal PrecioDetalles { get; set; }

    public virtual Pedido FkIdPedidosNavigation { get; set; } = null!;

    public virtual Producto FkIdProductoNavigation { get; set; } = null!;
}
