using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GameService
{
    [DataContract]
    public class WCFStaticData
    {
        public WCFStaticData()
        {
            DoType = new List<WCFDoType>();
            HomeType = new List<WCFHomeType>();
            OrderType = new List<WCFOrderType>();
            TerrainType = new List<WCFTerrainType>();
            TokenType = new List<WCFTokenType>();
            TrackType = new List<WCFTrackType>();
            UnitType = new List<WCFUnitType>();
            GamePoint = new List<WCFGamePoint>();
            ObjectPoint = new List<WCFObjectPoint>();
            TokenPoint = new List<WCFTokenPoint>();
            TrackPoint = new List<WCFTrackPoint>();
            Terrain = new List<WCFTerrain>();
            TerrainTerrain = new List<WCFTerrainTerrain>();
            Symbolic = new List<WCFSymbolic>();
            HomeCardType = new List<WCFHomeCardType>();
            VesterosCardType = new List<WCFVesterosCardType>();
            RandomDesk = new List<WCFRandomDesk>();
        }

        [DataMember]
        public List<WCFHomeType> HomeType { get; set; }
        [DataMember]
        public List<WCFOrderType> OrderType { get; set; }
        [DataMember]
        public List<WCFGamePoint> GamePoint { get; set; }
        [DataMember]
        public List<WCFTerrain> Terrain { get; set; }
        [DataMember]
        public List<WCFObjectPoint> ObjectPoint { get; set; }
        [DataMember]
        public List<WCFTerrainTerrain> TerrainTerrain { get; set; }
        [DataMember]
        public List<WCFTerrainType> TerrainType { get; set; }
        [DataMember]
        public List<WCFTokenPoint> TokenPoint { get; set; }
        [DataMember]
        public List<WCFTokenType> TokenType { get; set; }
        [DataMember]
        public List<WCFTrackPoint> TrackPoint { get; set; }
        [DataMember]
        public List<WCFTrackType> TrackType { get; set; }
        [DataMember]
        public List<WCFUnitType> UnitType { get; set; }
        [DataMember]
        public List<WCFSymbolic> Symbolic { get; set; }
        [DataMember]
        public List<WCFDoType> DoType { get; set; }
        [DataMember]
        public List<WCFHomeCardType> HomeCardType { get; set; }
        [DataMember]
        public List<WCFVesterosCardType> VesterosCardType { get; set; }
        [DataMember]
        public List<WCFRandomDesk> RandomDesk { get; set; }
    }
}
