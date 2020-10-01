using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFPowerCounter
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public string TokenType { get; set; }
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public string Terrain { get; set; }
    }
}
