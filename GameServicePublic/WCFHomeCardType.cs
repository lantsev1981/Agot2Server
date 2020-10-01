using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public partial class WCFHomeCardType
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string HomeType { get; set; }
        [DataMember]
        public int Strength { get; set; }
        [DataMember]
        public int Attack { get; set; }
        [DataMember]
        public int Defence { get; set; }
        [DataMember]
        public string Specialization { get; set; }
    }
}
