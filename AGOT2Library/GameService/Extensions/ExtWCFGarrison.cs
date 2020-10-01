using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFGarrison
    {
        static internal Garrison ToGarrison(this WCFGarrison o)
        {
            Garrison result = new Garrison();
            result.Id = o.Id;
            result.TokenType = o.TokenType;
            result.Terrain = o.Terrain;
            result.Strength = o.Strength;

            return result;
        }
    }
}
