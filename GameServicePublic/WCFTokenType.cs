using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFTokenType
    {
        [DataMember]
        public string Name { get; set; }
    }
}
