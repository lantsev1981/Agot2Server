using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace GamePortal
{
    [DataContract]
    public class WCFUser
    {
        public WCFUser()
        {
            this.UserGames = new List<WCFUserGame>();
            this.UserLikes = new List<WCFUserLike>();
            this.SpecialUsers = new List<WCFSpecialUser>();
            this.Title = new List<string>();
            this.Api = new Dictionary<string, string>();
        }        

        [DataMember]
        public bool IsIgnore { get; set; }
        [DataMember]
        public string Login { get; set; }
        [DataMember]
        public Guid Version { get; set; }
        [DataMember]
        public List<string> Title { get; set; }
        [DataMember]
        public WCFPayment LastPayment { get; set; }
        [DataMember]
        public List<WCFUserLike> UserLikes { get; set; }
        [DataMember]
        public List<WCFSpecialUser> SpecialUsers { get; set; }
        [DataMember]
        public List<WCFUserGame> UserGames { get; set; }
        [DataMember]
        public int AllPower { get; set; }
        [DataMember]
        public int Power { get; set; }
        [DataMember]
        public Dictionary<string, string> Api { get; set; }
        [DataMember]
        public Nullable<DateTimeOffset> LastConnection { get; set; }

        /// <summary>
        /// в избранном у пользователей
        /// </summary>
        [JsonIgnore]
        public List<string> SignerUsers { get; set; }
        [JsonIgnore]
        public string ClientId { get; set; }
    }
}
