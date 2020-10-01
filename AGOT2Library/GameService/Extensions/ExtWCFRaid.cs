using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFRaid
    {
        static internal Raid ToRaid(this WCFRaid o, Step step)
        {
            Raid result = new Raid();
            result.Step = step.Id;
            result.SourceOrder = o.SourceOrder;
            result.TargetOrder = o.TargetOrder;

            result.Step1 = step;

            return result;
        }
    }
}
