using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class Unit
    {
        public Unit() { }

        public Unit(GameUserInfo gameUserInfo, UnitType type, Terrain terrain)
        {
            this.Id = Guid.NewGuid();
            this.FirstId = this.Id;
            this.Step = gameUserInfo.Step;
            this.Game = gameUserInfo.Game;

            this.UnitType = type.Name;
            this.UnitType1 = type;
            this.Terrain = terrain.Name;
            this.Terrain1 = terrain;

            this.GameUserInfo = gameUserInfo;
            gameUserInfo.Unit.Add(this);
        }

        public WCFUnit ToWCFUnit()
        {
            WCFUnit result = new WCFUnit();
            result.Id = this.FirstId;
            result.Step = this.Step;
            result.UnitType = this.UnitType;
            result.Terrain = this.Terrain;
            result.IsWounded = this.IsWounded;

            return result;
        }

        public void CopyUnit(GameUserInfo gameUserInfo)
        {
            Unit result = new Unit(gameUserInfo, this.UnitType1, this.Terrain1);

            result.FirstId = this.FirstId;
            result.IsWounded = this.IsWounded;
        }
    }
}
