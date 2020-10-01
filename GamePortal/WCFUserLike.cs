using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GamePortal
{
    [DataContract]
    public class WCFUserLike
    {
        [DataMember]
        public string LikeLogin { get; set; }
        [DataMember]
        public bool IsLike { get; set; }
    }
}
