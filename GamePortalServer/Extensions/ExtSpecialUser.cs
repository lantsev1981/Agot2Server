using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamePortal
{
    public partial class SpecialUser
    {
        internal WCFSpecialUser ToWCFSpecialUser()
        {
            WCFSpecialUser result = new WCFSpecialUser();
            result.SpecialLogin = this.SpecialLogin;
            result.IsBlock = this.IsBlock;

            return result;
        }
    }
}
