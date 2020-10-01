using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class HomeType
    {
        public WCFHomeType ToWCFHomeType()
        {
            WCFHomeType result = new WCFHomeType();
            result.Name = this.Name;
            result.Terrain = this.Terrain;

            return result;
        }
    }
}
