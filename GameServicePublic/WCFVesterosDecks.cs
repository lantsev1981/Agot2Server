using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public partial class WCFVesterosDecks
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public string VesterosCardType { get; set; }
        [DataMember]
        public bool IsFull { get; set; }
        [DataMember]
        public string VesterosActionType { get; set; }
        [DataMember]
        public int Sort { get; set; }
    }
}
