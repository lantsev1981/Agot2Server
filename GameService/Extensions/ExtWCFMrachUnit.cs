using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFMarchUnit
    {
        static internal MarchUnit ToMarchUnit(this WCFMarchUnit o, March march)
        {
            MarchUnit result = new MarchUnit();

            result.March = march;

            result.Id = Guid.NewGuid();
            result.Step = march.Step;
            result.Game = march.Game;
            result.Unit = o.Unit;
            result.Terrain = o.Terrain;
            result.UnitType = o.UnitType;

            return result;
        }
    }
}
