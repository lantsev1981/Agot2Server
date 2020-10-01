using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFTerrainType
    {
        static internal TerrainType ToTerrainType(this WCFTerrainType o)
        {
            TerrainType result = new TerrainType();
            result.Name = o.Name;

            return result;
        }
    }
}
