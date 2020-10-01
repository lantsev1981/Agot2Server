using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFGame
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public System.DateTimeOffset CreateTime { get; set; }
        [DataMember]
        public Nullable<System.DateTimeOffset> OpenTime { get; set; }
        [DataMember]
        public Nullable<System.DateTimeOffset> CloseTime { get; set; }

        [DataMember]
        public List<WCFGameUser> GameUser { get; set; }

        [DataMember]
        public WCFGameSettings Settings { get; set; }
    }
}
