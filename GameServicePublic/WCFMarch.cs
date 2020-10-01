using System.Collections.Generic;
using System.Runtime.Serialization;


namespace GameService
{
    [DataContract]
    public partial class WCFMarch
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public string SourceOrder { get; set; }
        [DataMember]
        public bool IsTerrainHold { get; set; }

        [DataMember]
        public List<WCFMarchUnit> MarchUnit { get; set; }
    }
}
