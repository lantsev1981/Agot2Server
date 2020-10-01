using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFRaven
    {
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public string StepType { get; set; }
    }
}
