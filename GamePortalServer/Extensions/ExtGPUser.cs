using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamePortal
{
    public partial class User
    {
        public int Power { get { return this.Payments.Sum(p => p.Power); } }
        public int AllPower { get { return this.Payments.Where(p => p.Power > 0).Sum(p => p.Power); } }
        public Payment LastPayment { get { return this.Payments.FirstOrDefault(p => p.Time == this.Payments.Max(p1 => p1.Time)); } }//todo Max?
        public ApiUser LastApiUser { get { return this.ApiUsers.OrderBy(p => p.LastConnection).Last(); } }//todo или OrderBy

        public WCFUser ToWCFUser(GamePortalEntities dbContext)
        {
            WCFUser result = new WCFUser
            {
                Login = this.Login,
                IsIgnore = this.IsIgnore,
                Power = this.Power,
                AllPower = this.AllPower,
                Version = this.Version
            };

            var api = LastApiUser;
            result.LastConnection = api.LastConnection;
            //result.ClientId = api.ClientId;
            result.Api.Add("uid", api.uid);
            result.Api.Add("isFacebook", api.isFacebook.ToString());
            result.Api.Add("photo", api.photo);
            result.Api.Add("FIO", string.Format("{0} {1}", api.first_name, api.last_name));

            var time = DateTimeOffset.UtcNow - GamePortalServer.DataLiveTime;
#if DEBUG
            time = DateTimeOffset.MinValue;
#endif
            
            result.UserGames.AddRange(this.UserGames.Where(p => p.StartTime > time).Select(p => p.ToWCFUserGame()));
            result.UserLikes.AddRange(this.UserLikes.Where(p => p.Date > time).Select(p => p.ToWCFUserLike()));
            result.SpecialUsers.AddRange(this.SpecialUsers.Select(p => p.ToWCFSpecialUser()));
            result.SignerUsers = dbContext.SpecialUsers.Where(p => p.SpecialLogin == this.Login && !p.IsBlock).Select(p => p.Login).ToList();
            result.Title.AddRange(this.Titles.Select(p => p.Name));

            if (this.LastPayment != null && this.LastPayment.IsPublic)
            {
                result.LastPayment = new WCFPayment()
                {
                    Power = this.LastPayment.Power,
                    Time = this.LastPayment.Time,
                    Comment = this.LastPayment.Comment?.Substring(0, this.LastPayment.Comment.Length > 200 ? 200 : this.LastPayment.Comment.Length)
                };
            }

            return result;
        }
    }
}
