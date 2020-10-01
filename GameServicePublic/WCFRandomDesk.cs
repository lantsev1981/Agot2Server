using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public class WCFRandomDesk
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public int Strength { get; set; }
        [DataMember]
        public bool Attack { get; set; }
        [DataMember]
        public bool Defence { get; set; }
        [DataMember]
        public bool Skull { get; set; }
        [DataMember]
        public string fileName { get; set; }
    }
}
