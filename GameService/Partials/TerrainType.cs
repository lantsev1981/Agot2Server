using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class TerrainType
    {
        public WCFTerrainType ToWCFTerrainType()
        {
            WCFTerrainType result = new WCFTerrainType();
            result.Name = this.Name;

            return result;
        }
    }
}
