using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public partial class WCFVesterosCardType
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int DecksNumber { get; set; }
        [DataMember]
        public bool BarbarianFlag { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public int Count { get; set; }
    }
}
