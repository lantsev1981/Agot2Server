using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class TrackType
    {
        public WCFTrackType ToWCFTrackType()
        {
            WCFTrackType result = new WCFTrackType();
            result.Name = this.Name;

            return result;
        }
    }
}
