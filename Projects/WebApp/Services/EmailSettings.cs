namespace WebApp.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 25;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromName { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public bool UseSsl { get; set; } = false;
        public bool UseStartTls { get; set; } = true;
    }
}