using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFStep
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public System.Guid Game { get; set; }
        [DataMember]
        public string StepType { get; set; }
        [DataMember]
        public bool IsFull { get; set; }
        [DataMember]
        public System.Guid GameUser { get; set; }
        [DataMember]
        public ICollection<string> Message { get; set; }


        [DataMember]
        public WCFGameInfo GameInfo { get; set; }
        [DataMember]
        public WCFGameUserInfo GameUserInfo { get; set; }
        [DataMember]
        public WCFBattleUser BattleUser { get; set; }
        [DataMember]
        public WCFMarch March { get; set; }
        [DataMember]
        public WCFRaid Raid { get; set; }
        [DataMember]
        public WCFRaven Raven { get; set; }
        [DataMember]
        public WCFSupport Support { get; set; }
        [DataMember]
        public WCFVoting Voting { get; set; }
        [DataMember]
        public virtual WCFVesterosAction VesterosAction { get; set; }        
        [DataMember]
        public List<ArrowModel> ArrowModelList { get; set; }
        [DataMember]
        public bool? IsNeedSupport { get; set; }
    }
}
