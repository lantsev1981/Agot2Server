using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFUnitType
    {
        static internal UnitType ToUnitType(this WCFUnitType o)
        {
            UnitType result = new UnitType();
            result.Name = o.Name;
            result.TokenType = o.TokenType;
            result.Strength = o.Strength;
            result.Cost = o.Cost;
            result.Count = o.Count;

            return result;
        }
    }
}
