using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFRaid
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public Nullable<System.Guid> TargetOrder { get; set; }
        [DataMember]
        public System.Guid SourceOrder { get; set; }
    }
}
