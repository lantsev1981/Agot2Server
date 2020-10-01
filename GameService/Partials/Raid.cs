using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class Raid
    {
        public WCFRaid ToWCFRaid()
        {
            WCFRaid result = new WCFRaid();
            result.Step = this.Step;
            result.SourceOrder = this.SourceOrder;
            result.TargetOrder = this.TargetOrder;

            return result;
        }

        //public Raid NewRaid(Guid stepId)
        //{
        //    Raid result = new Raid();
        //    result.SourceOrder = this.SourceOrder;
        //    result.TargetOrder = this.TargetOrder;

        //    return result;
        //}
    }
}
