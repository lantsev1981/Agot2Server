using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFSupport
    {
        static internal Support ToSupport(this WCFSupport o, Step step)
        {
            Support result = new Support();
            result.Step = step.Id;
            result.Game = step.Game;
            result.BattleId = o.BattleId;
            result.SupportUser = o.SupportUser;

            result.Step1 = step;

            return result;
        }
    }
}
