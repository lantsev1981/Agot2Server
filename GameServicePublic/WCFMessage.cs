using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public partial class WCFMessage
    {
        [DataMember]
        public string Value { get; set; }
    }
}
