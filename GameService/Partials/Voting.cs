using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class Voting
    {
        public Voting() { }

        public Voting(Step step, string target)
        {
            this.Step1 = step;

            this.Step = step.Id;
            this.Game = step.Game;
            this.Target = target;

            step.Voting = this;
        }

        internal WCFVoting ToWCFVoting()
        {
            WCFVoting result = new WCFVoting();
            result.Step = this.Step;
            result.Target = this.Target;            
            if (this.Step1.GameUser1.Game1.LastHomeSteps.Where(p => p.StepType == "Борьба_за_влияние").All(p => p.IsFull))
                result.PowerCount = this.PowerCount;

            return result;
        }

        internal void CopyTo(Step step)
        {
            Voting result = new Voting(step, this.Target);
            result.PowerCount = this.PowerCount;
        }
    }
}
