using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class Support
    {
        public WCFSupport ToWCFSupport()
        {
            WCFSupport result = new WCFSupport();
            result.Step = this.Step;
            result.SupportUser = this.SupportUser;
            result.BattleId = this.BattleId;

            return result;
        }

        public void CopySupport(Step step)
        {
            Support result = new Support();

            result.Step1 = step;

            result.Step = step.Id;
            result.Game = step.Game;
            result.SupportUser = this.SupportUser;
            result.BattleId = this.BattleId;

            step.Support = result;
        }
    }
}
