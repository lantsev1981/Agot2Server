using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFGameException
    {
        [DataMember]
        public string Game { get; set; }
        [DataMember]
        public string Login { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string stackTrace { get; set; }
    }
}
