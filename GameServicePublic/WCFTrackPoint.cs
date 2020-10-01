using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFTrackPoint
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public string TrackType { get; set; }
        [DataMember]
        public int Value { get; set; }
        [DataMember]
        public System.Guid GamePoint { get; set; }
    }
}
