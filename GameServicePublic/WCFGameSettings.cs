using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

using GamePortal;

namespace GameService
{
    [DataContract]
    public class WCFGameSettings
    {
        public WCFGameSettings()
        {
            RateSettings = new WCFRateSettings();
        }

        [DataMember]
        public string CreatorLogin { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public bool HasPassword { get; set; }
        [DataMember]
        public int GameType { get; set; }
        [DataMember]
        public int RandomIndex { get; set; }
        [DataMember]
        public bool IsRandomSkull { get; set; }
        [DataMember]
        public WCFRateSettings RateSettings { get; set; }
        
        [DataMember]
        public int MaxTime { get; set; }
        [DataMember]
        public int AddTime { get; set; }
        [DataMember]
        public string Lang { get; set; }
        [DataMember]
        public bool WithoutChange { get; set; }
        [DataMember]
        public bool IsGarrisonUp { get; set; }
        [DataMember]
        public bool NoTimer { get; set; }

        public string LangImage { get { return Lang == null ? $"/Image/all_lang.png" : $"/Image/{Lang}.png"; } }

        public bool CheckInput()
        {
            if (string.IsNullOrEmpty(CreatorLogin) || CreatorLogin == "System" || CreatorLogin == "Вестерос")
                return false;
            if (RateSettings == null || RateSettings.MindRate < 0 || RateSettings.HonorRate < 0 || RateSettings.DurationRate < 0)
                return false;

            return true;
        }
    }
}
