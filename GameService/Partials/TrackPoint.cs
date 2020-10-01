using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class TrackPoint
    {
        public WCFTrackPoint ToWCFTrackPoint()
        {
            WCFTrackPoint result = new WCFTrackPoint();
            result.Id = this.Id;
            result.TrackType = this.TrackType;
            result.GamePoint = this.GamePoint;
            result.Value = this.Value;

            return result;
        }
    }
}
