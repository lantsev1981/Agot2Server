using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public class ArrowModel
    {
        public ArrowModel()
        {
            FirstId = Guid.NewGuid();
        }

        [DataMember]
        public Guid FirstId { get; set; }
        [DataMember]
        public string StartTerrainName { get; set; }
        [DataMember]
        public string EndTerrainName { get; set; }
        [DataMember]
        public ArrowType ArrowType { get; set; }
    }

    public enum ArrowType
    {
        March,
        Attack,
        Support,
        Retreat,
    }
}
