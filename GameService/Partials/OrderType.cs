using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class OrderType
    {
        public WCFOrderType ToWCFOrderType()
        {
            WCFOrderType result = new WCFOrderType();
            result.Name = this.Name;
            result.TokenType = this.TokenType;
            result.IsSpecial = this.IsSpecial;
            result.Strength = this.Strength;
            result.Count = this.Count;
            result.DoType = this.DoType;

            return result;
        }
    }
}
