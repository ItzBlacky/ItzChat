using System.Text.Json;

namespace ItzChat
{
    public class Message
    {
        public readonly string Type;
        public readonly string[] Data;

        public Message(string Type, string[] Data)
        {
            this.Type = Type;
            this.Data = Data;
        }

        public string toJson()
        {
            return JsonSerializer.Serialize(this);
        }
        public static Message fromJson(string Json)
        {
            try
            {
                return JsonSerializer.Deserialize<Message>(Json);
            } catch(JsonException)
            {
                return null;
            }
        }
    }
}
