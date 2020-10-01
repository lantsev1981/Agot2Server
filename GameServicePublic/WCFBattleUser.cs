using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFBattleUser
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public System.Guid BattleId { get; set; }
        [DataMember]
        public string HomeCardType { get; set; }
        [DataMember]
        public string AdditionalEffect { get; set; }
        [DataMember]
        public Nullable<System.Guid> RandomDeskId { get; set; }
        [DataMember]
        public Nullable<int> Strength { get; set; }
        [DataMember]
        public Nullable<bool> IsWinner { get; set; }
    }
}
