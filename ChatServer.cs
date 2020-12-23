using System;
using System.Collections.Generic;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ItzChat
{
    public class ChatServer : WebSocketBehavior
    {

        private readonly AuthHandler auth;
        private readonly ItzContext db;
        private User self;
        public ChatServer(AuthHandler auth, ItzContext db) : base()
        {
            this.auth = auth;
            this.db = db;
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            if (!e.IsText || e.Data.IsNullOrEmpty() || e.Data == "{}")
            {
                // Sends error code 402 (Bad Request)
                SendCode("402");
                return;
            }
            Message message = Message.FromJson(e.Data);
            if (message is null)
            {
                Console.WriteLine("Invalid message");
                SendCode("402");
                return;
            }

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
                // Sends error code 406 (Unauthorized)
                SendCode("406");
                return;
            }

            if (message.Data.Length < 1)
            {
                SendCode("402");
                return;
            }

            if (message.Type == "SENDTOUSERNAME")
            {
                // Data = ["authkey", "username to send to", "message"]
                if (message.Data.Length != 3 || message.Data[0].Length != 2048)
                {
                    SendCode("402");
                    return;
                }
                if (!auth.VerifyConnection(Context.WebSocket, message.Data[0]))
                {
                    SendCode("406");
                    return;
                }
                WebSocket ToSend = auth.GetConnection(message.Data[1]);
                if (ToSend is null)
                {
                    SendCode("405");
                    return;
                }
                ToSend.Send(new Message("MESSAGE", new string[] { self.Id.ToString(), self.UserName, message.Data[2] }).ToJson());
                Send("300");
                return;
            }

            if(message.Type == "SENDTOGROUP")
            {
                // Data = ["authkey", "groupname to send to", "message"]
                if (message.Data.Length != 3 || message.Data[0].Length != 2048)
                {
                    SendCode("404");
                    return;
                }
                if (!auth.VerifyConnection(Context.WebSocket, message.Data[0]))
                {
                    SendCode("406");
                    return;
                }

                Group group = db.Groups.FirstOrDefault(x => x.Name == message.Data[1]);

                if(group is null)
                {
                    SendCode("404");
                    return;
                }
                
                foreach(User user in group.Members)
                {
                    WebSocket toSend = auth.GetConnection(user);
                    if(toSend is null) 
                        continue;
                    toSend.Send(new Message("GROUPMESSAGE",
                                                    new string[] { 
                                                        group.Id.ToString(),
                                                        group.Name,
                                                        message.Data[2] 
                                                    }).ToJson());
                }

                SendCode("300");
                return;
            }

            if(message.Type == "CREATEGROUP")
            {
                // Data = ["authkey", "group name"]
                if(message.Data.Length != 2 || message.Data[0].Length != 2048)
                {
                    SendCode("402");
                    return;
                }
                if(!auth.VerifyConnection(Context.WebSocket, message.Data[0]))
                {
                    SendCode("406");
                    return;
                }
                if(!db.Groups.Any(x => x.Name == message.Data[1]))
                {
                    SendCode("404");
                    return;
                }
                Group group = new Group() { Name = message.Data[1], Owner = self, Admins = new List<User>(), Members = new List<User>() };
                db.Groups.Add(group);
                db.SaveChangesAsync();
                SendCode("300");
                return;
            }

            if(message.Type == "ADDUSERNAMETOGROUP")
            {
                // Data = ["authkey", "groupname", "user1", "user2", ...]
                if(message.Data.Length < 3 || message.Data[0].Length != 2048)
                {
                    SendCode("402");
                    return;
                }
                if(!auth.VerifyConnection(Context.WebSocket, message.Data[0]))
                {
                    SendCode("406");
                    return;
                }
                
                Group group = db.Groups.FirstOrDefault(x => x.Name == message.Data[1]);

                if(group is null)
                {
                    SendCode("404");
                    return;
                }
                if(!group.Admins.Any(x => x.Equals(self)))
                {
                    SendCode("406");
                    return;
                }
                for(int i = 2; i < message.Data.Length; i++)
                {
                    User user = db.Users.FirstOrDefault(x => x.UserName == message.Data[i]);
                    if(user is null) 
                        continue;
                    group.Members.Add(user);
                }
                db.SaveChangesAsync();
                SendCode("300");
                return;
            }

            if(message.Type == "SETMEMBEROFGROUPASADMIN")
            {
                // Data = ["authkey", "groupname", "user"]
                if(message.Data.Length != 3 || message.Data[0].Length != 2048)
                {
                    SendCode("402");
                    return;
                }
                if(!auth.VerifyConnection(Context.WebSocket, message.Data[0]))
                {
                    SendCode("406");
                    return;
                }

                Group group = db.Groups.FirstOrDefault(x => x.Name == message.Data[0]);
                
                if(group is null)
                {
                    SendCode("404");
                    return;
                }
                if(!group.Owner.Equals(self))
                {
                    SendCode("406");
                    return;
                }

                User toAdd = group.Members.FirstOrDefault(x => x.UserName == message.Data[2]);
                
                if(toAdd is null)
                {
                    SendCode("404");
                    return;
                }
                group.Admins.Add(toAdd);
                db.SaveChangesAsync();
                SendCode("300");
                return;
            }

            Console.WriteLine($"Message Type not found: {message.Type}");
        }
        
        private void SendCode(params string[] code)
        {
            Send(new Message("RESPONSE", code).ToJson());
        }
    }
}