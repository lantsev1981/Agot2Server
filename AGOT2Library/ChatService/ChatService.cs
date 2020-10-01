using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ServiceModel.Web;
using System.IO;
using System.ServiceModel.Channels;
using MyLibrary;
using SuperSocket.WebSocket;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using GameService;
using Agot2Server;

namespace ChatServer
{
    public partial class ChatService : WebSocketModel, IDisposable
    {
        public int LengthArray { get; private set; }

        WebSocketServer appServer;
        List<Chat> _Chats = new List<Chat>();

        public ChatService(Guid gameId, int lengthArray)
        {
            LengthArray = lengthArray;
            this.Name = "ChatService";
            this.GameId = gameId;

            Start();
        }

        private void Start()
        {
            try
            {
#if !DEBUG
                this.Port = 7000;
#endif
#if DEBUG
                this.Port = 4444;
#endif

                if (this.GameId != Guid.Empty)
                    this.Port = new Random().Next(System.Net.IPEndPoint.MaxPort - 7001) + 7001;
                else
                { }
                appServer = new WebSocketServer();
                if (!appServer.Setup(this.Port)) //Setup with listening port
                    throw new Exception("ChatService: Failed to setup!");

                appServer.NewSessionConnected += AppServer_NewSessionConnected;
                appServer.SessionClosed += AppServer_SessionClosed;
                appServer.NewMessageReceived += AppServer_NewMessageReceived;

                if (!appServer.Start())
                    throw new Exception("ChatService: Failed to start!");
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.Message);
                Start();
            }
        }

        public override void Dispose()
        {
            appServer.Stop();
            appServer.Dispose();
        }

        private void AppServer_NewMessageReceived(WebSocketSession session, string value)
        {
            var chat = JsonConvert.DeserializeObject<Chat>(value);
            var hex = Crypto.SHA1Hex($"{chat.Creator}{chat.Message}{Guid.Empty}");
            if (chat.SHA1Hex != hex)
                throw new WebFaultException(System.Net.HttpStatusCode.BadRequest);

            chat.SHA1Hex = null;
            AddChat(chat);
        }

        public void AddChat(Chat chat)
        {
            //chat.Id = Guid.NewGuid();
            chat.Time = DateTimeOffset.UtcNow;

            lock (_Chats)
            {
                if (_Chats.Count == LengthArray)
                    _Chats.RemoveAt(0);

                _Chats.Add(chat);
            }

            foreach (var item in appServer.GetAllSessions().ToList())
            {
                var result = new ChatServiceMethod() { Method = "item", Value = JsonConvert.SerializeObject(chat) };
                new Task(() => item.Send(JsonConvert.SerializeObject(result))).Start();
            }
        }

        private void AppServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            System.Diagnostics.Debug.WriteLine($"ChatService: Client disconnected! Sessions counter = {appServer.SessionCount}");
        }

        private void AppServer_NewSessionConnected(WebSocketSession session)
        {
            System.Diagnostics.Debug.WriteLine($"ChatService: New session connected! Sessions counter = {appServer.SessionCount}");

            lock (_Chats)
            {
                var result = new ChatServiceMethod() { Method = "items", Value = JsonConvert.SerializeObject(_Chats) };
                new Task(() => session.Send(JsonConvert.SerializeObject(result))).Start();
            }
        }
    }
}
