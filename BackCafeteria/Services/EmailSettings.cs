namespace BackCafeteria.Services
{
    public class EmailSettings
    {
        public string CorreoRemitente { get; set; } = "";
        public string ClaveApp { get; set; } = "";
        public string SmtpServidor { get; set; } = "";
        public int SmtpPuerto { get; set; }
        public bool UsarSSL { get; set; }
    }
}
