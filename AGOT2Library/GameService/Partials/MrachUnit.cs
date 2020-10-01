using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class MarchUnit
    {   
        public Terrain LocalTerrain
        {
            get { return this.March.Step1.GameUser1.Game1.DbContext.Terrain.Single(p => p.Name == this.Terrain); }
        }

        public UnitType LocalUnitType
        {
            get { return this.March.Step1.GameUser1.Game1.DbContext.UnitType.Single(p => p.Name == this.UnitType); }
        }

        internal WCFMarchUnit ToMarchUnit()
        {
            WCFMarchUnit result = new WCFMarchUnit();
            result.Id = Guid.NewGuid();
            result.Step = this.Step;
            result.Terrain = this.Terrain;
            result.Unit = this.Unit;
            result.UnitType = this.UnitType;

            return result;
        }
    }
}
