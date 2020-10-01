using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace GamePortal
{
    [DataContract]
    public class user
    {
        //[JsonIgnore]
        [DataMember]
        public string login { get; set; }
        [JsonIgnore]
        [DataMember]
        public string uid { get; set; }
        [JsonIgnore]
        [DataMember]
        public bool isFacebook { get; set; }

        [JsonIgnore]
        [DataMember]
        public string first_name { get; set; }

        [JsonIgnore]
        [DataMember]
        public string last_name { get; set; }

        [JsonIgnore]
        [DataMember]
        public string photo { get; set; }

        //[JsonIgnore]
        [DataMember]
        public string email { get; set; }

        [JsonIgnore]
        [DataMember]
        public bool emailConfirm { get; set; }

        [JsonIgnore]
        [DataMember]
        public string clientId { get; set; }
    }
}
