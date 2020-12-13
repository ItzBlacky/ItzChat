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
     * 405 
     * 406 = not authenticated
     * 407 = already authenticated
     * 408 = 
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
                AuthHandler handler = new AuthHandler(db);
                var server = new WebSocketServer("ws://0.0.0.0:811");
                server.AddWebSocketService("/chat", () => new ChatServer(handler));
                server.Start();
                Console.ReadKey();
                server.Stop();
            }

        }
    }
    public class ChatServer : WebSocketBehavior {
        
        private AuthHandler auth;
        public ChatServer(AuthHandler handler) : base()
        {
            this.auth = handler;
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            // return if data is not a string
            if (!e.IsText) return;
            Message message;
            try
            {
                // deserialize
                message = Message.fromJson(e.Data);
            } catch(JsonException)
            {
                // Sends error code 402 (Bad Request)
                Send(new Message("RESPONSE", new string[]{ "402" }).toJson());
                return;
            }


            // Checks if connection is authenticated
            if(!auth.Authenticated(Context.WebSocket))
            {
                // Checks if received message type is of "LOGIN"
                if(message.Type == "LOGIN")
                {
                    // Pass Login to Handler
                    auth.HandleLogin(Context.WebSocket, message);
                    return;
                }
                // Checks if received message type is of "REGISTER"
                if(message.Type == "REGISTER")
                {
                    // Pass Register to Handler
                    auth.HandleRegister(Context.WebSocket, message);
                    return;
                }
                // Sends error code 406 (Not Authenticated)
                Send(new Message("Response", new string[] { "406"}).toJson());
                return;
            }



        }
    }
    public class AuthHandler
    {
        //
        private Dictionary<string, WebSocket> connections;
        private ItzContext db;
        public AuthHandler(ItzContext db)
        {
            connections = new Dictionary<string, WebSocket>();
            this.db = db;
        }
        public void HandleLogin(WebSocket socket, Message message)
        {
            List<string> toReturn = new List<string>();
            if (message.Data.Length != 2) toReturn.Add("402");
            if (connections.ContainsValue(socket)) toReturn.Add("407");
            string username = message.Data[0];
            string password = message.Data[1];
            User user = db.Users.FirstOrDefault(user => user.UserName == username);
            if(user is null)
            {
                toReturn.Add("404");
            } else if(PasswordHash.ArgonHashStringVerify(Encoding.UTF8.GetBytes(password), Convert.FromBase64String(user.Password))) {
                string authstring = GenerateRandomString();
                connections.Add(authstring, socket);
                toReturn.Add("300");
                toReturn.Add(authstring);
            }
            socket.Send(new Message("AUTHRESPONSE", toReturn.ToArray()).toJson());
        }
        public void HandleRegister(WebSocket socket, Message message)
        {
            List<string> toReturn = new List<string>();
            if (message.Data.Length != 3) toReturn.Add("402");
            if (connections.ContainsValue(socket)) toReturn.Add("407");
            string username = message.Data[0];
            string password = message.Data[1];
            string email = message.Data[2];
            if (db.Users.Any(x => (x.UserName == username) || (x.Email == email))) toReturn.Append("403");
            else if (username.IsNullOrEmpty() || password.IsNullOrEmpty() || email.IsNullOrEmpty()) toReturn.Append("402");
            else
            {
                string hashed = Convert.ToBase64String(PasswordHash.ArgonHashBinary(Encoding.UTF8.GetBytes(password),
                    PasswordHash.ArgonGenerateSalt(),
                    PasswordHash.StrengthArgon.Medium,
                    256, PasswordHash.ArgonAlgorithm.Argon_2ID13));
                User user = new User() { UserName = username, Email = email, Password = hashed };
                db.Users.Add(user);
                db.SaveChangesAsync();
                toReturn.Append("300");
                string authstring = GenerateRandomString();
                connections.Add(authstring, socket);
                toReturn.Append(authstring);
            }
            socket.Send(new Message("AUTHRESPONSE", toReturn.ToArray()).toJson());
        }
        public bool Authenticated(WebSocket socket)
        {
            return connections.Any(x => x.Value == socket);
        }
        public bool verifyConnection(WebSocket socket, string authstring)
        {
            return connections.Any(x => x.Value == socket && x.Key == authstring);
        }
        private string GenerateRandomString(int length = 2048)
        {
            StringBuilder str = new StringBuilder();
            
            while(true)
            {
                if(str.Length < length)
                {
                    str.Append(Convert.ToBase64String(PasswordHash.ArgonGenerateSalt()));
                    continue;
                }
                break;
            }
            return str.ToString().Substring(0, length-1);
        }
    }
}
