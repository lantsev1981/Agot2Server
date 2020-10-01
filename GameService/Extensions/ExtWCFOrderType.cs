using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFOrderType
    {
        static internal OrderType ToOrderType(this WCFOrderType o)
        {
            OrderType result = new OrderType();
            result.Name = o.Name;
            result.TokenType = o.TokenType;
            result.IsSpecial = o.IsSpecial;
            result.Strength = o.Strength;
            result.Count = o.Count;
            result.DoType = o.DoType;

            return result;
        }
    }
}
