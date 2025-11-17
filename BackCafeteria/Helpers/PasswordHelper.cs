namespace BackCafeteria.Helpers
{
    public class PasswordHelper
    {
        public static string GenerarContraseñaTemporal(int longitud = 10)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$%&*";
            var random = new Random();

            return new string(Enumerable
                .Repeat(chars, longitud)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }

    }
}
