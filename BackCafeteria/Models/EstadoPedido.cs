using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackCafeteria.Models
{
    [Table("EstadoPedidos")]
    public partial class EstadoPedido
    {
        [Key]
        [Column("id_estado")]
        public int IdEstado { get; set; }

        [Column("nombre_pedido")]
        public string NombrePedido { get; set; } = null!;

        public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}