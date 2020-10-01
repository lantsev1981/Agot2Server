using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class ObjectPoint
    {
        public WCFObjectPoint ToWCFObjectPoint()
        {
            WCFObjectPoint result = new WCFObjectPoint();
            result.Id = this.Id;
            result.Terrain = this.Terrain;
            result.HomeType = this.HomeType;
            result.Symbolic = this.Symbolic;
            result.GamePoint = this.GamePoint;
            result.Sort = this.Sort;

            return result;
        }
    }
}
