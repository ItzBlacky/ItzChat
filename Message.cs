using System.Text.Json;

namespace ItzChat
{
    public class Message
    {
        public readonly string Type;
        public readonly string[] Data;
        public readonly string Sender;

        public Message(string Type, string[] Data, string Sender = "")
        {
            this.Type = Type;
            this.Data = Data;
            this.Sender = Sender;
        }

        public string toJson()
        {
            return JsonSerializer.Serialize(this);
        }
        public static Message fromJson(string Json)
        {
            return JsonSerializer.Deserialize<Message>(Json);
        }
    }
}
