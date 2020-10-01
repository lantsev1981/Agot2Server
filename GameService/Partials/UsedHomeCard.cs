using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class UsedHomeCard
    {
        public UsedHomeCard() { }

        public UsedHomeCard(GameUserInfo gameUserInfo, HomeCardType homeCardType)
        {
            this.GameUserInfo = gameUserInfo;

            this.Step = gameUserInfo.Step;
            this.Game = gameUserInfo.Game;
            this.HomeCardType = homeCardType.Name;
            this.HomeCardType1 = homeCardType;

            gameUserInfo.UsedHomeCard.Add(this);
        }

        internal WCFUsedHomeCard ToWCFUsedHomeCard()
        {
            WCFUsedHomeCard result = new WCFUsedHomeCard();
            result.Step = this.Step;
            result.HomeCardType = this.HomeCardType;

            return result;
        }

        internal void CopyUsedHomeCard(GameUserInfo gameUserInfo)
        {
            new UsedHomeCard(gameUserInfo, this.HomeCardType1);
        }
    }
}
