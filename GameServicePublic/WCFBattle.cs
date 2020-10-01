using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFBattle
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public string AttackTerrain { get; set; }
        [DataMember]
        public string DefenceTerrain { get; set; }
        [DataMember]
        public System.Guid AttackUser { get; set; }
        [DataMember]
        public System.Guid DefenceUser { get; set; }
        [DataMember]
        public bool IsAttackUserNeedSupport { get; set; }
        [DataMember]
        public bool? IsDefenceUserNeedSupport { get; set; }
    }
}
