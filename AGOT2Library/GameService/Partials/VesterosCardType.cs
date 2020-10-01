using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameService
{
    public partial class VesterosCardType
    {
        public WCFVesterosCardType ToWCFVesterosCardType()
        {
            WCFVesterosCardType result = new WCFVesterosCardType();
            result.BarbarianFlag = this.BarbarianFlag;
            result.Count = this.Count;
            result.DecksNumber = this.DecksNumber;
            result.Description = this.Description;
            result.Id = this.Id;
            result.Name = this.Name;

            return result;
        }
    }
}
