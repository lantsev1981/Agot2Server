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
    public class WCFGamePortal : List<WCFUser>
    {
        /*/// <summary>
        /// все подключения
        /// </summary>
        [JsonIgnore]
        public List<WCFUserGame> UserGames { get; private set; }
        /// <summary>
        /// завершённые подключения
        /// </summary>
        [JsonIgnore]
        public List<WCFUserGame> EndedUserGames { get; private set; }
        /// <summary>
        /// завершённые игры
        /// </summary>
        [JsonIgnore]
        public List<WCFUserGame> EndedGames { get; private set; }


        [JsonIgnore]
        public int MindRate { get; private set; }

        [JsonIgnore]
        public int HonorRate { get; private set; }

        [JsonIgnore]
        public double LikeRate { get; private set; }

        [JsonIgnore]
        public int MinLikeRate { get; private set; }
        [JsonIgnore]
        public int MaxLikeRate { get; private set; }
        [JsonIgnore]
        private int _SubLikes;
        [JsonIgnore]
        public double DurationHours { get; private set; }
        [JsonIgnore]
        public double DurationDay { get; private set; }


        public void Update()
        {
            UserGames = this.Where(p => !p.IsIgnore).SelectMany(p => p.UserGames).ToList();
            EndedUserGames = this.UserGames.Where(p => p.MindPosition != 0).ToList();
            EndedGames = this.EndedUserGames.Where(p => p.MindPosition == 1).ToList();

            var userGames = this.EndedUserGames.Where(p => !p.IsIgnoreMind).ToList();
            MindRate = userGames.Count == 0 ? 0 : (int)userGames.Average(p => p.MindRate);

            userGames = this.UserGames.Where(p => !p.IsIgnoreHonor).ToList();
            HonorRate = userGames.Count == 0 ? 0 : (int)userGames.Average(p => p.HonorRate);

            var users = this.Where(p => p.LikeRate != 0).ToList();
            LikeRate = userGames.Count == 0 ? 0 : users.Average(p => p.LikeRate);

            MinLikeRate = this.Count == 0 ? 0 : this.Min(p => p.LikeRate);
            MaxLikeRate = this.Count == 0 ? 0 : this.Max(p => p.LikeRate);
            _SubLikes = MaxLikeRate - MinLikeRate;
            DurationHours = this.Sum(p => p.DurationHours);
            DurationDay = this.DurationHours / 24d;
        }
        public int GetLike(WCFUser user)
        {
            if (_SubLikes == 0)
                return 100;

            return ((user.LikeRate - MinLikeRate) * 100) / _SubLikes;
        }

        public int GetAbsolute(WCFUser user)
        {
            var likeRatio = GetLike(user);
            return (user.MindRate + user.HonorRate + likeRatio) / 3;
        }

        public bool CheckRateAllow(WCFRateSettings rateSettings, WCFUser user)
        {
            //TODO проверить на сервере проходит ли пользователь по рейтингу
            return 
            //this.MindRate >= rateSettings.MindRate 
            //    && user.HonorRate >= rateSettings.HonorRate 
            //    && user.LikeRate >= rateSettings.LikeRate
            //    && user.DurationHours >= rateSettings.DurationRate 
                GetAbsolute(user) >= rateSettings.DurationRate
                ? true : false;
        }

        /// <summary>
        /// Возвращает рейтинг побед дома
        /// </summary>
        /// <param name="homeType"></param>
        /// <returns></returns>
        public HomeTypeRate GetHomeTypeRate(string homeType)
        {
            HomeTypeRate result = new HomeTypeRate();
            result.GamesCount = this.EndedGames.Where(p => p.HomeType == homeType).Count();
            result.Value = this.EndedGames.Count == 0 ? 0 : (result.GamesCount / (double)this.EndedGames.Count) * 100;
            result.HomeType = homeType;
            return result;
        }

        public HomeTypeRate GetHomeMind(string homeType, bool onlyFullGame = true)
        {
            var endedGames = this.EndedUserGames.GroupBy(p => p.GameId);
            var result = onlyFullGame ? endedGames.Where(p => p.Count() == 6).ToList() : endedGames.ToList();
            return new HomeTypeRate() { Value = result.Average(p => MindFunc(p, homeType)) / 3, GamesCount = result.Count, HomeType = homeType };
        }

        private int MindFunc(IEnumerable<WCFUserGame> p, string s)
        {
            var home = p.SingleOrDefault(p1 => p1.HomeType == s);
            return home == null ? 0 : home.MindRate.Value;
        }*/
    }

    /*[DataContract]
    public class HomeTypeRate
    {
        /// <summary>
        /// рейтинг за дом
        /// </summary>
        [DataMember]
        public double Value { get; set; }

        /// <summary>
        /// Завершённых игр за дом
        /// </summary>
        [DataMember]
        public int GamesCount { get; set; }

        public string HomeType { get; set; }
    }*/
}
