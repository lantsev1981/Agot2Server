using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public partial class WCFVesterosAction
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public Nullable<int> ActionNumber { get; set; }
        [DataMember]
        public System.Guid VesterosDecks { get; set; }
    }
}
