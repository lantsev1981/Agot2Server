using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFRaven
    {
        static internal Raven ToRaven(this WCFRaven o, Step step)
        {
            Raven result = new Raven();
            result.Step = step.Id;
            result.Game = step.Game;
            result.StepType = o.StepType;

            result.Step1 = step;

            return result;
        }
    }
}
