using System.Text.Json;
using System;

namespace ItzChat
{
    public class Message
    {
        public string Type { get; private set; }
        public string[] Data { get; private set; }

        public Message(string Type, string[] Data)
        {
            this.Type = Type;
            this.Data = Data;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
        public static Message FromJson(string Json)
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
