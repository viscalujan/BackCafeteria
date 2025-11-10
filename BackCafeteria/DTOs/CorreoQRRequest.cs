namespace BackCafeteria.Models
{
    public class CorreoQRRequest
    {
        public string CorreoRemitente { get; set; }
        public string ClaveApp { get; set; }
        public string SmtpServidor { get; set; }
        public int SmtpPuerto { get; set; }
        public bool UsarSSL { get; set; }
        public string CorreoDestino { get; set; }
        public string Huella { get; set; }
        public string HuellaBase64 { get; set; }
    }
}
