using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFPowerCounter
    {
        static internal PowerCounter ToPowerCounter(this WCFPowerCounter o)
        {
            PowerCounter result = new PowerCounter();
            result.Id = o.Id;
            result.TokenType = o.TokenType;
            result.Terrain = o.Terrain;

            return result;
        }
    }
}
