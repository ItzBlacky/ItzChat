using System;
using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ItzChat
{
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
            if (e.Data.IsNullOrEmpty() || e.Data == "{}")
            {
                // Sends error code 402 (Bad Request)
                Send(new Message("RESPONSE", new string[] { "402" }).ToJson());
                return;
            }
            Message message = Message.FromJson(e.Data);
            Console.WriteLine("5");
            if (message is null)
            {
                Console.WriteLine("Invalid message");
                Send(new Message("RESPONSE", new string[] { "402" }).ToJson());
                return;
            }

            Console.WriteLine("Valid Message");
            // Checks if connection is authenticated
            if (!auth.Authenticated(Context.WebSocket))
            {
                Console.WriteLine("Not authenticated");
                // Checks if received message type is of "LOGIN"
                if (message.Type == "LOGIN")
                {
                    // Pass Login to Handler
                    auth.HandleLogin(Context.WebSocket, message);
                    self = auth.GetUser(Context.WebSocket);
                    return;
                }
                // Checks if received message type is of "REGISTER"
                if (message.Type == "REGISTER")
                {
                    // Pass Register to Handler
                    auth.HandleRegister(Context.WebSocket, message);
                    self = auth.GetUser(Context.WebSocket);
                    return;
                }
                // Sends error code 406 (Not Authenticated)
                Console.WriteLine("Not Authenticated 2");
                Send(new Message("RESPONSE", new string[] { "406" }).ToJson());
                return;
            }
            Console.WriteLine("Authenticated");
            if (message.Data.Length < 1)
            {
                Console.WriteLine("Message length is less than one");
                Send(new Message("RESPONSE", new string[] { "402" }).ToJson());
                return;
            }

            if (message.Type == "SENDTOUSERNAME")
            {
                if (message.Data.Length != 3 || message.Data[0].Length != 2048)
                {
                    Send(new Message("RESPONSE", new string[] { "402" }).ToJson());
                    return;
                }
                if (!auth.VerifyConnection(Context.WebSocket, message.Data[0]))
                {
                    Send(new Message("RESPONSE", new string[] { "406" }).ToJson());
                    return;
                }
                WebSocket ToSend = auth.GetConnection(message.Data[1]);
                if (ToSend is null)
                {
                    Send(new Message("RESPONSE", new string[] { "404" }).ToJson());
                    return;
                }
                ToSend.Send(new Message("MESSAGE", new string[] { self.Id.ToString(), self.UserName, message.Data[2] }).ToJson());
                return;
            }

            Console.WriteLine($"Message Type not found: {message.Type}");
        }
    }
}