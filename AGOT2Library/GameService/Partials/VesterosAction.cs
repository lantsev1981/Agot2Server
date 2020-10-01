using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class VesterosAction
    {
        internal WCFVesterosAction ToWCFVesterosAction()
        {
            WCFVesterosAction result = new WCFVesterosAction();
            result.Step = this.Step;
            result.VesterosDecks = this.VesterosDecks;

            return result;
        }
    }
}
