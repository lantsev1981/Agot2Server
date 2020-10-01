using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GamePortal
{
    [DataContract]
    public class WCFSpecialUser
    {
        [DataMember]
        public string SpecialLogin { get; set; }
        [DataMember]
        public bool IsBlock { get; set; }
    }
}
