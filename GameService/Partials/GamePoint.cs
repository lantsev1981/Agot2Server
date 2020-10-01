using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class GamePoint
    {
        public WCFGamePoint ToWCFGamePoint()
        {
            WCFGamePoint result = new WCFGamePoint();
            result.Id = this.Id;
            result.X = this.X;
            result.Y = this.Y;

            return result;
        }
    }
}
