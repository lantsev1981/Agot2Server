using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class PowerCounter
    {
        public WCFPowerCounter ToWCFPowerCounter()
        {
            WCFPowerCounter result = new WCFPowerCounter();
            result.Id = this.Id;
            result.TokenType = this.TokenType;
            result.Step = this.Step;
            result.Terrain = this.Terrain;

            return result;
        }

        public void CopyPowerCounter(GameUserInfo gameUserInfo)
        {
            PowerCounter result = new PowerCounter();

            result.GameUserInfo = gameUserInfo;
            result.Terrain1 = this.Terrain1;

            result.Id = Guid.NewGuid();
            result.Step = gameUserInfo.Step;
            result.Game = gameUserInfo.Game;
            result.TokenType = this.TokenType;
            result.Terrain = this.Terrain;

            gameUserInfo.PowerCounter.Add(result);
        }
    }
}
