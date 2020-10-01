using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFTokenPoint
    {
        static internal TokenPoint ToTokenPoint(this WCFTokenPoint o)
        {
            TokenPoint result = new TokenPoint();
            result.Id = o.Id;
            result.Terrain = o.Terrain;
            result.GamePoint = o.GamePoint;
            result.TokenType = o.TokenType;

            return result;
        }
    }
}
