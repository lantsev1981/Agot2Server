using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameService
{
    public partial class HomeCardType
    {
        public WCFHomeCardType ToWCFHomeCardType()
        {
            WCFHomeCardType result = new WCFHomeCardType();
            result.Name = this.Name;
            result.HomeType = this.HomeType;
            result.Strength = this.Strength;
            result.Attack = this.Attack;
            result.Defence = this.Defence;
            result.Specialization = this.Specialization;

            return result;
        }
    }
}
