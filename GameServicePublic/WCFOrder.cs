using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFOrder
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public string OrderType { get; set; }
        [DataMember]
        public string Terrain { get; set; }
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public bool IsFull { get; set; }
    }
}
