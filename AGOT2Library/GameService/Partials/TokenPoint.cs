using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class TokenPoint
    {
        public WCFTokenPoint ToWCFTokenPoint()
        {
            WCFTokenPoint result = new WCFTokenPoint();
            result.Id = this.Id;
            result.Terrain = this.Terrain;
            result.GamePoint = this.GamePoint;
            result.TokenType = this.TokenType;

            return result;
        }
    }
}
