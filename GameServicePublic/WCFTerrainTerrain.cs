using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFTerrainTerrain
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public string Terrain { get; set; }
        [DataMember]
        public string JoinTerrain { get; set; }
    }
}
