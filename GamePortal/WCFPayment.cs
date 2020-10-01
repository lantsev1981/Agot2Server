using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace GamePortal
{
    [DataContract]
    public class WCFPayment
    {
        [DataMember]
        public DateTimeOffset Time { get; set; }
        [DataMember]
        public int Power { get; set; }
        [DataMember]
        public string Comment { get; set; }
    }
}
