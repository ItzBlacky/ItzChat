using Sodium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ItzChat
{
    /*
     * 300 = success
     * 402 = bad request
     * 403 = username or email in use
     * 404 = not found
     * 405 = wrong credential
     * 406 = not authenticated
     * 407 = already authenticated
     * 
     * 
     * 
     */
    class Program
    {
        static void Main(string[] args)
        {
            using(ItzContext db = new ItzContext())
            {
                db.Database.EnsureCreated();
                AuthHandler handler = new AuthHandler(db);
                var server = new WebSocketServer("ws://0.0.0.0:811");
                server.AddWebSocketService("/chat", () => new ChatServer(handler));
                server.Start();
                Console.ReadKey();
                server.Stop();
            }

        }
    }
    public class ChatServer : WebSocketBehavior 
    {
        
        private AuthHandler auth;
        private User self;
        public ChatServer(AuthHandler handler) : base()
        {
            auth = handler;
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            // return if data is not a string
            if (!e.IsText)
            {
                // Sends error code 402 (Bad Request)
                Send(new Message("RESPONSE", new string[] { "402" }).ToJson());
                return;
            }
            if(e.Data.IsNullOrEmpty() || e.Data == "{}")
            {
                // Sends error code 402 (Bad Request)
                Send(new Message("RESPONSE", new string[] { "402" }).ToJson());
                return;
            }
            Message message;
            Console.WriteLine($"\n\n\n{e.Data}\n\n");
            try
            {
                Console.WriteLine("1");
                message =  JsonSerializer.Deserialize<Message>(e.Data);
                Console.WriteLine("2");
            } catch(JsonException)
            {
                Console.WriteLine("3");
                message = new Message("", new string[] { "" });
                Console.WriteLine("4");
            }
            Console.WriteLine("5");
            if(message is null)
            {
                Console.WriteLine("Invalid message");
                Send(new Message("RESPONSE", new string[] { "402" }).ToJson());
                return;
            }

            Console.WriteLine("Valid Message");
            // Checks if connection is authenticated
            if(!auth.Authenticated(Context.WebSocket))
            {
                Console.WriteLine("Not authenticated");
                // Checks if received message type is of "LOGIN"
                if(message.Type == "LOGIN")
                {
                    // Pass Login to Handler
                    auth.HandleLogin(Context.WebSocket, message);
                    self = auth.GetUser(Context.WebSocket);
                    return;
                }
                // Checks if received message type is of "REGISTER"
                if(message.Type == "REGISTER")
                {
                    // Pass Register to Handler
                    auth.HandleRegister(Context.WebSocket, message);
                    self = auth.GetUser(Context.WebSocket);
                    return;
                }
                // Sends error code 406 (Not Authenticated)
                Console.WriteLine("Not Authenticated 2");
                Send(new Message("RESPONSE", new string[] { "406"}).ToJson());
                return;
            }
            Console.WriteLine("Authenticated");
            if(message.Data.Length < 1)
            {
                Console.WriteLine("Message length is less than one");
                Send(new Message("RESPONSE", new string[]{ "402" }).ToJson());
                return;
            }

            if(message.Type == "SENDTOUSERNAME")
            {
                if(message.Data.Length != 3 || message.Data[0].Length != 2048)
                {
                    Send(new Message("RESPONSE", new string[]{ "402" }).ToJson());
                    return;
                }
                if(!auth.VerifyConnection(Context.WebSocket, message.Data[0]))
                {
                    Send(new Message("RESPONSE", new string[] {"406"}).ToJson());
                    return;
                }
                WebSocket ToSend = auth.GetConnection(message.Data[1]);
                if(ToSend is null) 
                {
                    Send(new Message("RESPONSE", new string[] {"404"}).ToJson());
                    return;
                }
                ToSend.Send(new Message("MESSAGE", new string[] { self.Id.ToString(), self.UserName, message.Data[2] }).ToJson());
                return;
            }

            Console.WriteLine($"Message Type not found: {message.Type}");
        }
    }
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
                else if (PasswordHash.ArgonHashStringVerify(Convert.FromBase64String(user.Password),
                                                            Encoding.UTF8.GetBytes(password)))
                {
                    Console.WriteLine("13");
                    string authstring = GenerateRandomString();
                    connections.Add((user, authstring, socket));
                    toReturn.Add("300");
                    toReturn.Add(authstring);
                } else
                {
                    Console.WriteLine("14");
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
                    string hashed = Convert.ToBase64String(PasswordHash.ArgonHashBinary(Encoding.UTF8.GetBytes(password),
                        PasswordHash.ArgonGenerateSalt(),
                        PasswordHash.StrengthArgon.Medium,
                        256, PasswordHash.ArgonAlgorithm.Argon_2ID13));
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
            while(str.Length < length)
            {
                str.Append(Convert.ToBase64String(PasswordHash.ArgonGenerateSalt()));
                str.Replace("+", "");
            }
            return str.ToString().Substring(0, length);
        }
        public void Flush()
        {   
            Console.WriteLine("Flushing..");
            try {
            foreach(var entry in connections)
            {
                if(!entry.socket.IsAlive)
                {
                    Console.WriteLine($"Removing connection of user with username {entry.user.UserName}");
                    connections.Remove(entry);
                    Console.WriteLine("0");
                }
                Console.WriteLine("1");
            }
            } catch(Exception)
                    {
                        Console.WriteLine("e");
                    }
            Console.WriteLine("2");
        }
    }
}
