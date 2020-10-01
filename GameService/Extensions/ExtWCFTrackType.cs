using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFTrackType
    {
        static internal TrackType ToTrackType(this WCFTrackType o)
        {
            TrackType result = new TrackType();
            result.Name = o.Name;

            return result;
        }
    }
}
