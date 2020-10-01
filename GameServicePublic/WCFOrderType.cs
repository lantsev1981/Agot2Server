using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFOrderType
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string TokenType { get; set; }
        [DataMember]
        public bool IsSpecial { get; set; }
        [DataMember]
        public int Strength { get; set; }
        [DataMember]
        public int Count { get; set; }
        [DataMember]
        public string DoType { get; set; }
    }
}
