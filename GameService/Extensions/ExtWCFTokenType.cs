using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFTokenType
    {
        static internal TokenType ToTokenType(this WCFTokenType o)
        {
            TokenType result = new TokenType();
            result.Name = o.Name;

            return result;
        }
    }
}
