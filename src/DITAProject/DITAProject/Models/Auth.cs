using System;

namespace ITAJira.Models
{
    public class Auth
    {
        public Auth(string server, string user, string password)
        {
            Server = server;
            User = user;
            Password = password;
        }

        public string Server { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        internal int GetHash()
        {
            return HashCode.Combine(Server, User);
        }

        internal bool CheckFields()
        {
            return !(string.IsNullOrWhiteSpace(Server) || string.IsNullOrWhiteSpace(User) || string.IsNullOrWhiteSpace(Password));
        }
    }
}
