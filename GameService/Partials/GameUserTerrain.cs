using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class GameUserTerrain
    {
        public WCFGameUserTerrain ToWCFGameUserTerrain()
        {
            WCFGameUserTerrain result = new WCFGameUserTerrain();
            result.Id = this.Id;
            result.Step = this.Step;
            result.Terrain = this.Terrain;

            return result;
        }

        public void CopyGameUserTerrain(GameUserInfo gameUserInfo)
        {
            GameUserTerrain result = new GameUserTerrain();

            result.GameUserInfo = gameUserInfo;

            result.Id = Guid.NewGuid();
            result.Step = gameUserInfo.Step;
            result.Game = gameUserInfo.Game;
            result.Terrain = this.Terrain;
            result.Terrain1 = this.Terrain1;

            gameUserInfo.GameUserTerrain.Add(result);
        }
    }
}
