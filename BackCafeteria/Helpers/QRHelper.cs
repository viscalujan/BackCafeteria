
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BackCafeteria.Helpers
{
    public class QRHelper
    {
        public static (string contenidoQR, byte[] imagenQR, string hashQR) GenerarCodigoQR(string numeroControl)
        {
            // Usa UTC para evitar problemas de huso horario
            string contenidoOriginal = $"{numeroControl}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            string hash = Sha256Hex(contenidoOriginal);

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(hash, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new BitmapByteQRCode(qrData);
            byte[] imagenBytes = qrCode.GetGraphic(20); // PNG

            return (contenidoOriginal, imagenBytes, hash);
        }

        private static string Sha256Hex(string texto)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(texto);
            return Convert.ToHexString(sha.ComputeHash(bytes)); // 64 chars
        }

    }
}
