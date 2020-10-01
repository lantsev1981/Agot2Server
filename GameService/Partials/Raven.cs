using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class Raven
    {
        public WCFRaven ToWCFRaven()
        {
            WCFRaven result = new WCFRaven();
            result.Step = this.Step;
            result.StepType = this.StepType;

            return result;
        }

        public void CopyRaven(Step step)
        {
            Raven result = new Raven();

            result.Step1 = step;

            result.Step = step.Id; 
            result.Game = step.Game;
            result.StepType = this.StepType;

            step.Raven = result;
        }
    }
}
