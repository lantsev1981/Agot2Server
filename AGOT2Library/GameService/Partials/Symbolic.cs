using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameService
{
    public partial class Symbolic
    {
        public WCFSymbolic ToWCFSymbolic()
        {
            WCFSymbolic result = new WCFSymbolic();
            result.Name = this.Name;

            return result;
        }
    }
}
