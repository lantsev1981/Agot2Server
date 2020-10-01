using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFMarch
    {
        static internal March ToMarch(this WCFMarch o, Step step)
        {
            March result = new March();

            result.Step1 = step;

            result.Step = step.Id;
            result.SourceOrder = o.SourceOrder;
            result.IsTerrainHold = o.IsTerrainHold;

            foreach (var item in o.MarchUnit)
                result.MarchUnit.Add(item.ToMarchUnit(result));

            return result;
        }
    }
}
