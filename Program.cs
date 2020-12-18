using System;
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
}
