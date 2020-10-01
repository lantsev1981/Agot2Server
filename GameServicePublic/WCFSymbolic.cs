using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFSymbolic
    {
        [DataMember]
        public string Name { get; set; }
    }
}
