using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public partial class WCFUsedHomeCard
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public string HomeCardType { get; set; }
    }
}
