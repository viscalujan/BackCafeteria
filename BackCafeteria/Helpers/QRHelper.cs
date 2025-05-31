
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
            string contenidoOriginal = $"{numeroControl}_{DateTime.Now:yyyyMMddHHmmss}";
            string hash = GenerarHashSHA256(contenidoOriginal);

            // Generar imagen QR con el hash
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(hash, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new BitmapByteQRCode(qrData);
            byte[] imagenBytes = qrCode.GetGraphic(20);

            return (contenidoOriginal, imagenBytes, hash);
        }

        private static string GenerarHashSHA256(string texto)
        {
            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));
            return Convert.ToHexString(hashBytes); // desde .NET 5+ / .ToHexString es más limpio que BitConverter
        }
    
    }
}
