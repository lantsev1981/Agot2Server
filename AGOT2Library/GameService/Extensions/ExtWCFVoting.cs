using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;



namespace GameService
{
    static internal class ExtWCFVoting
    {
        static internal void CopyTo(this WCFVoting o, Step step)
        {
            Voting result = new Voting(step, o.Target);
            result.PowerCount = o.PowerCount;
        }
    }
}
