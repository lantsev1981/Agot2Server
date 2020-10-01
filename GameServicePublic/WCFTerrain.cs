using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFTerrain
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string TerrainType { get; set; }
        [DataMember]
        public int Supply { get; set; }
        [DataMember]
        public int Power { get; set; }
        [DataMember]
        public int Strength { get; set; }
    }
}
