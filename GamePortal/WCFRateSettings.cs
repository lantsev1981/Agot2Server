using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GamePortal
{
    [DataContract]
    public class WCFRateSettings
    {
        [DataMember]
        public int MindRate { get; set; }
        [DataMember]
        public int HonorRate { get; set; }
        [DataMember]
        public int LikeRate { get; set; }
        [DataMember]
        public int DurationRate { get; set; }
    }
}
