using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFDoType
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int Sort { get; set; }
    }
}
