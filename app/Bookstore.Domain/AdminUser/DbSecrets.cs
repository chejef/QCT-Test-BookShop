namespace Bookstore.Domain.AdminUser
{
    public class DbSecrets
    {
        public string Host { get; set; } = null!;

        public int Port { get; set; }

        public string Username { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string DbInstanceIdentifier { get; set; } = null!;

        public string Engine { get; set; } = null!;
    }
}