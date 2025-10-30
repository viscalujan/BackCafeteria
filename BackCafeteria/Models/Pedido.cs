using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("Pedidos")]
    public partial class Pedido
    {
        [Key]
        [Column("id_pedidos")]
        public int IdPedidos { get; set; }

        [Column("FK_id_usuario")]
        public int FkIdUsuario { get; set; }

        [Column("fecha_pedido")]
        public DateTime FechaPedido { get; set; }

        [Column("total_pedido")]
        public decimal? TotalPedido { get; set; }

        [Column("FK_id_estado")]
        public int FkIdEstado { get; set; }

        [ForeignKey("FkIdUsuario")]
        public virtual Usuario FkIdUsuarioNavigation { get; set; } = null!;

        [ForeignKey("FkIdEstado")]
        public virtual EstadoPedido FkIdEstadoNavigation { get; set; } = null!;

        public virtual ICollection<PedidoDetalle> PedidoDetalles { get; set; } = new List<PedidoDetalle>();
    }
}