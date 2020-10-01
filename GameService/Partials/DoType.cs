using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class DoType
    {
        public WCFDoType ToWCFDoType()
        {
            WCFDoType result = new WCFDoType();
            result.Name = this.Name;
            result.Sort = this.Sort;

            return result;
        }
    }
}
