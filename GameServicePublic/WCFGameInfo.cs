using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public partial class WCFGameInfo
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public int Turn { get; set; }
        [DataMember]
        public int Barbarian { get; set; }

        [DataMember]
        public ICollection<WCFGarrison> Garrison { get; set; }
        [DataMember]
        public WCFBattle Battle { get; set; }
        [DataMember]
        public ICollection<WCFVesterosDecks> VesterosDecks { get; set; }
    }
}
