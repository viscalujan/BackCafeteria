using System.Text.Json.Serialization;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Correo { get; set; }

    [JsonPropertyName("NumeroControl")]
    public string Contra { get; set; }
    
    [JsonPropertyName("CodigoQR")]
    public byte[]? Huella { get; set; }

    public decimal Credito { get; set; }

    [JsonIgnore] // el rol no se muestra, como pediste
    public string Rol { get; set; }
    public string CodigoQRTexto { get; set; }
    public string Contrasena { get; set; }

}
