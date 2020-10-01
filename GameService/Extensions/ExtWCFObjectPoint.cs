using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFObjectPoint
    {
        static internal ObjectPoint ToObjectPoint(this WCFObjectPoint o)
        {
            ObjectPoint result = new ObjectPoint();
            result.Id = o.Id;
            result.Terrain = o.Terrain;
            result.HomeType = o.HomeType;
            result.Symbolic = o.Symbolic;
            result.GamePoint = o.GamePoint;
            result.Sort = o.Sort;

            return result;
        }
    }
}
