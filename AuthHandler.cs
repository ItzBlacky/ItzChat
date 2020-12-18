using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using WebSocketSharp;
using Sodium;

namespace ItzChat
{
    public class AuthHandler
    {
        private List<(User user, string token, WebSocket socket)> connections;
        private ItzContext db;
        public AuthHandler(ItzContext db)
        {
            connections = new List<(User, string, WebSocket)>();
            this.db = db;
        }
        public void HandleLogin(WebSocket socket, Message message)
        {
            Flush();
            Console.WriteLine("Handling Login");
            List<string> toReturn = new List<string>();
            if (message.Data.Length != 2)
                toReturn.Add("402");
            else if (connections.Any(x => x.socket == socket))
                toReturn.Add("407");
            else
            {
                Console.WriteLine("11");
                string username = message.Data[0];
                string password = message.Data[1];
                User user = db.Users.FirstOrDefault(user => user.UserName == username);
                if (user is null)
                {
                    Console.WriteLine("12");
                    toReturn.Add("404");
                }
                else if (PasswordHash.ArgonHashStringVerify(Encoding.UTF8.GetString(Convert.FromBase64String(user.Password)), password))
                {
                    string authstring = GenerateRandomString();
                    connections.Add((user, authstring, socket));
                    toReturn.Add("300");
                    toReturn.Add(authstring);
                }
                else
                { }
                    toReturn.Add("405");
                }
            }
            Console.WriteLine("15");
            socket.Send(new Message("AUTHRESPONSE", toReturn.ToArray()).ToJson());
        }
        public void HandleRegister(WebSocket socket, Message message)
        {
            Flush();
            List<string> toReturn = new List<string>();
            if (message.Data.Length != 3)
                toReturn.Add("402");
            else if (connections.Any(x => x.socket == socket))
                toReturn.Add("407");
            else
            {
                string username = message.Data[0];
                string password = message.Data[1];
                string email = message.Data[2];
                if (db.Users.Any(x => (x.UserName == username) || (x.Email == email)))
                    toReturn.Add("403");
                else if (username.IsNullOrEmpty() || password.IsNullOrEmpty() || email.IsNullOrEmpty())
                    toReturn.Add("402");
                else
                {
                    string hashed = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                                                            PasswordHash.ArgonHashString(password, PasswordHash.StrengthArgon.Medium)));
                    User user = new User() { UserName = username, Email = email, Password = hashed };
                    db.Users.Add(user);
                    db.SaveChangesAsync();
                    toReturn.Add("300");
                    string authstring = GenerateRandomString();
                    connections.Add((user, authstring, socket));
                    toReturn.Add(authstring);
                }
            }

            socket.Send(new Message("AUTHRESPONSE", toReturn.ToArray()).ToJson());
        }
        public bool Authenticated(WebSocket socket)
        {
            Flush();
            return connections.Any(x => x.socket.Equals(socket));
        }
        public bool VerifyConnection(WebSocket socket, string authstring)
        {
            Flush();
            return connections.Any(x => x.socket.Equals(socket) && x.token == authstring);
        }
        public WebSocket GetConnection(string username)
        {
            return connections.FirstOrDefault(x => x.user.UserName == username).socket;
        }
        public User GetUser(WebSocket socket)
        {
            return connections.FirstOrDefault(x => x.socket.Equals(socket)).user;
        }
        public static string GenerateRandomString(int length = 2048)
        {
            StringBuilder str = new StringBuilder();
            while (str.Length < length)
            {
                str.Append(Convert.ToBase64String(PasswordHash.ArgonGenerateSalt()));
                str.Replace("+", "");
            }
            return str.ToString().Substring(0, length);
        }
        public void Flush()
        {
            Console.WriteLine("Flushing..");
            try
            {
                foreach (var entry in connections)
                {
                    if (!entry.socket.IsAlive)
                    {
                        Console.WriteLine($"Removing connection of user with username {entry.user.UserName}");
                        connections.Remove(entry);
                        Console.WriteLine("0");
                    }
                    Console.WriteLine("1");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("e");
            }
            Console.WriteLine("2");
        }
    }
}
