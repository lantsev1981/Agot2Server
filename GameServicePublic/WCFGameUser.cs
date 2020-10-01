using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFGameUser
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public System.Guid Game { get; set; }
        [DataMember]
        public string Login { get; set; }
        [DataMember]
        public string HomeType { get; set; }
        [DataMember]
        public bool OnLineStatus { get; set; }
        [DataMember]
        public int LastChatIndex { get; set; }
        [DataMember]
        public int LastStepIndex { get; set; }
        [DataMember]
        public bool NewStep { get; set; }
        [DataMember]
        public bool NewChat { get; set; }
        [DataMember]
        public bool NeedReConnect { get; set; }
        [DataMember]
        public bool IsCapitulated { get; set; }
    }
}
