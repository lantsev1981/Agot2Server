using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class March
    {
        public WCFMarch ToWCFMarch()
        {
            WCFMarch result = new WCFMarch();
            result.Step = this.Step;
            result.SourceOrder = this.SourceOrder;
            result.IsTerrainHold = this.IsTerrainHold;

            result.MarchUnit = new List<WCFMarchUnit>();
            foreach (var item in this.MarchUnit)
                result.MarchUnit.Add(item.ToMarchUnit());

            return result;
        }
    }
}
