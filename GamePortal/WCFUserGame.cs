using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

using System.Windows.Media;
using System.Xml.Serialization;

namespace GamePortal
{
    [DataContract]
    public class WCFUserGame
    {
        [DataMember]
        public Guid Id { get; set; }
        [DataMember]
        public Guid GameId { get; set; }
        [DataMember]
        public int GameType { get; set; }
        [DataMember]
        public string HomeType { get; set; }
        [DataMember]
        public DateTimeOffset StartTime { get; set; }
        [DataMember]
        public Nullable<DateTimeOffset> EndTime { get; set; }
        [DataMember]
        public int MindPosition { get; set; }
        [DataMember]
        public bool IsIgnoreMind { get; set; }
        [DataMember]
        public int HonorPosition { get; set; }
        [DataMember]
        public bool IsIgnoreHonor { get; set; }
        [DataMember]
        public bool IsIgnoreDurationHours;
    }
}
