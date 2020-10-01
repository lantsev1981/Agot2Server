using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class TokenType
    {
        public WCFTokenType ToWCFTokenType()
        {
            WCFTokenType result = new WCFTokenType();
            result.Name = this.Name;

            return result;
        }
    }
}
