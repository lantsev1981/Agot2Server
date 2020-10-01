using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFTerrain
    {
        static internal Terrain ToTerrain(this WCFTerrain o)
        {
            Terrain result = new Terrain();
            result.Name = o.Name;
            result.TerrainType = o.TerrainType;
            result.Supply = o.Supply;
            result.Power = o.Power;
            result.Strength = o.Strength;

            return result;
        }
    }
}
