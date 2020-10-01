using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class Garrison
    {
        public WCFGarrison ToWCFGarrison()
        {
            WCFGarrison result = new WCFGarrison();
            result.Id = this.Id;
            result.Step = this.Step;
            result.TokenType = this.TokenType;
            result.Terrain = this.Terrain;
            result.Strength = this.Strength;

            return result;
        }

        public void CopyGarrison(GameInfo gameInfo)
        {
            Garrison result = new Garrison();

            result.GameInfo = gameInfo;
            result.Terrain1 = this.Terrain1;
            result.TokenType1 = this.TokenType1;

            result.Id = Guid.NewGuid();
            result.Step = gameInfo.Step;
            result.Game = gameInfo.Game;
            result.TokenType = this.TokenType;
            result.Terrain = this.Terrain;
            result.Strength = this.Strength;

            gameInfo.Garrison.Add(result);
        }
    }
}
