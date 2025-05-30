﻿namespace CafeteriaAPI.Models
{
    public class VentaDetalle
    {
        public int Id { get; set; }

        public int VentaId { get; set; }

        public int ProductoId { get; set; }

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public Producto? Producto { get; set; }

        public Venta? Venta { get; set; }


    }
}
