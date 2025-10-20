using System;
using System.Collections.Generic;

namespace BackCafeteria.Models;

public partial class VentaDetalle
{
    public int IdVdetalle { get; set; }

    public int FkIdVenta { get; set; }

    public int FkIdProducto { get; set; }

    public int CantidadProducto { get; set; }

    public decimal PrecioUnitario { get; set; }

    public virtual Producto FkIdProductoNavigation { get; set; } = null!;

    public virtual Venta FkIdVentaNavigation { get; set; } = null!;
}
