using Newtonsoft.Json;
using System;

namespace ChatServer
{
    public partial class Chat
    {
        public DateTimeOffset Time { get; set; }
        public string Creator { get; set; }
        public string Message { get; set; }
        public string SHA1Hex { get; set; }

        [JsonIgnore]
        public bool IsVisible { get; set; }
    }

    public partial class ChatServiceMethod
    {
        public string Method { get; set; }
        public string Value { get; set; }
    }
}
