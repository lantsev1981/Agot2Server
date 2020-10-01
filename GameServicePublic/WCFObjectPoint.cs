using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFObjectPoint
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public string Terrain { get; set; }
        [DataMember]
        public string Symbolic { get; set; }
        [DataMember]
        public string HomeType { get; set; }
        [DataMember]
        public System.Guid GamePoint { get; set; }
        [DataMember]
        public int Sort { get; set; }        
    }
}
