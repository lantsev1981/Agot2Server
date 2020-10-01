using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamePortal
{
    public partial class UserLike
    {
        internal WCFUserLike ToWCFUserLike()
        {
            WCFUserLike result = new WCFUserLike();
            result.LikeLogin = this.LikeLogin;
            result.IsLike = this.IsLike;

            return result;
        }
    }
}
