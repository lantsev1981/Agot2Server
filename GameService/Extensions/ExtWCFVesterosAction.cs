using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;



namespace GameService
{
    static internal class ExtWCFVesterosAction
    {
        static internal VesterosAction ToVesterosAction(this WCFVesterosAction o, Step step)
        {
            VesterosAction result = new VesterosAction();

            result.Step1 = step;

            result.Step = step.Id;
            result.VesterosDecks = o.VesterosDecks;
            result.ActionNumber = o.ActionNumber;

            return result;
        }
    }
}
