using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class TerrainTerrain
    {
        public WCFTerrainTerrain ToWCFTerrainTerrain()
        {
            WCFTerrainTerrain result = new WCFTerrainTerrain();
            result.Id = this.Id;
            result.JoinTerrain = this.JoinTerrain;
            result.Terrain = this.Terrain;

            return result;
        }
    }
}
