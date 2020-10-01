using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFSymbolic
    {
        static internal Symbolic ToSymbolic(this WCFSymbolic o)
        {
            Symbolic result = new Symbolic();
            result.Name = o.Name;

            return result;
        }
    }
}
