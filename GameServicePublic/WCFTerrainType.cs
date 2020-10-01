using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFTerrainType
    {
        [DataMember]
        public string Name { get; set; }
    }
}
