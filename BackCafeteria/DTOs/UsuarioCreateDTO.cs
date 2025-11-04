public class UsuarioCreateDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string NumeroControl { get; set; } = null!;
    public decimal Credito { get; set; }
    public string Rol { get; set; } = null!;
    public string? Contra { get; set; } // Para registrar contraseña
    public string? CodigoQRTexto { get; set; }
}

public class AumentoCreditoDTO
{
    public string NumeroControl { get; set; } = null!;
    public decimal Cantidad { get; set; }
}

