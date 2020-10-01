using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class UnitType
    {
        public WCFUnitType ToWCFUnitType()
        {
            WCFUnitType result = new WCFUnitType();
            result.Name = this.Name;
            result.TokenType = this.TokenType;
            result.Strength = this.Strength;
            result.Cost = this.Cost;
            result.Count = this.Count;

            return result;
        }
    }
}
