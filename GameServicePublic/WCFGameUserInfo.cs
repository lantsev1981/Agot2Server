using System.Collections.Generic;

using System.Runtime.Serialization;

namespace GameService
{
    [DataContract]
    public partial class WCFGameUserInfo
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public int Power { get; set; }
        [DataMember]
        public int Supply { get; set; }
        [DataMember]
        public int ThroneInfluence { get; set; }
        [DataMember]
        public int BladeInfluence { get; set; }
        [DataMember]
        public int RavenInfluence { get; set; }
        [DataMember]
        public bool IsBladeUse { get; set; }

        [DataMember]
        public ICollection<WCFGameUserTerrain> GameUserTerrain { get; set; }
        [DataMember]
        public ICollection<WCFOrder> Order { get; set; }
        [DataMember]
        public List<WCFPowerCounter> PowerCounter { get; set; }
        [DataMember]
        public List<WCFUnit> Unit { get; set; }
        [DataMember]
        public List<WCFUsedHomeCard> UsedHomeCard { get; set; }

        public int SpecialOrderCount(int playerCount)
        {
            if (playerCount < 5)
                switch (RavenInfluence)
                {
                    case 1: return 3;
                    case 2: return 2;
                    case 3: return 1;
                    default: return 0;
                }
            else
                switch (RavenInfluence)
                {
                    case 1: return 3;
                    case 2: return 3;
                    case 3: return 2;
                    case 4: return 1;
                    default: return 0;
                }
        }
    }
}
