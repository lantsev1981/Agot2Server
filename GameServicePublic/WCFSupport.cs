using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFSupport
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public Nullable<System.Guid> SupportUser { get; set; }
        [DataMember]
        public System.Guid BattleId { get; set; }
    }
}
