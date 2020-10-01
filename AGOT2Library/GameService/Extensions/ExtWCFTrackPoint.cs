using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFTrackPoint
    {
        static internal TrackPoint ToTrackPoint(this WCFTrackPoint o)
        {
            TrackPoint result = new TrackPoint();
            result.Id = o.Id;
            result.TrackType = o.TrackType;
            result.GamePoint = o.GamePoint;
            result.Value = o.Value;

            return result;
        }
    }
}
