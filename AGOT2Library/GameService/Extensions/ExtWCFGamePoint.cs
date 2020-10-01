using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFGamePoint
    {
        static internal GamePoint ToGamePoint(this WCFGamePoint o)
        {
            GamePoint result = new GamePoint();
            result.Id = o.Id;
            result.X = o.X;
            result.Y = o.Y;

            return result;
        }
    }
}
