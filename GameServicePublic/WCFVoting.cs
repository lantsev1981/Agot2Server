using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public partial class WCFVoting
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public string Target { get; set; }
        [DataMember]
        public int PowerCount { get; set; }
    }
}
