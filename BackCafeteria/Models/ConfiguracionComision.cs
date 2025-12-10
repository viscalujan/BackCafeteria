using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("ConfiguracionComisiones")]
public class ConfiguracionComision
{
    [Key]
    [Column("id_config")]
    public int IdConfig { get; set; }

    [Column("metodo_pago")]
    [MaxLength(20)]
    public string MetodoPago { get; set; } = null!;   // 'credito', 'pedido'

    [Column("porcentaje")]
    public decimal Porcentaje { get; set; }           // ej. 10.00, 3.00

    [Column("minimo")]
    public decimal Minimo { get; set; }               // mínimo absoluto

    [Column("umbral")]
    public decimal Umbral { get; set; }               // desde qué monto aplica %

    [Column("activo")]
    public bool Activo { get; set; } = true;
}
