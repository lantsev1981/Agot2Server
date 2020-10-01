using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFHomeType
    {
        static internal HomeType ToHomeType(this WCFHomeType o)
        {
            HomeType result = new HomeType();
            result.Name = o.Name;
            result.Terrain = o.Terrain;

            return result;
        }
    }
}
