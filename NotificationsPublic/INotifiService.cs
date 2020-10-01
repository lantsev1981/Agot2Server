using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

using System.Drawing;
using System.Xml.Serialization;
using GameService;

namespace Notifications
{
    public delegate NotifiResult GetGameListDelegate(string clientVersion, string clientId);

    [ServiceContract]
    public interface INotifiServer
    {
        [OperationContract]
        NotifiResult GetGameList(string clientVersion, string clientId);
    }

    [DataContract]
    public class NotifiResult
    {
        [DataMember]
        public int ClientCount { get; set; }
        [DataMember]
        public List<Notifi> NotifiList { get; set; }
    }

    [DataContract]
    public class Notifi
    {
        [DataMember]
        public string Content { get; set; }
        [DataMember]
        public WCFGame Game { get; set; }
        [DataMember]
        public string User { get; set; }
        [DataMember]
        public NotifiSettings Settings { get; set; }
    }

    [DataContract]
    public class NotifiSettings
    {
        [DataMember]
        public Size Size { get; set; }
        [DataMember]
        public bool IsCenter { get; set; }

        [DataMember]
        public TimeSpan StartTime { get; set; }

        [DataMember]
        public TimeSpan ShowTime { get; set; }

        [DataMember]
        public TimeSpan RepeatTime { get; set; }
    }
}
