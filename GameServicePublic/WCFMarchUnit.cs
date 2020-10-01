﻿using System;
using System.Collections.Generic;

using System.Runtime.Serialization;
using System.Text;


namespace GameService
{
    [DataContract]
    public partial class WCFMarchUnit
    {
        [DataMember]
        public System.Guid Id { get; set; }
        [DataMember]
        public int Step { get; set; }
        [DataMember]
        public System.Guid Unit { get; set; }
        [DataMember]
        public string Terrain { get; set; }
        [DataMember]
        public string UnitType { get; set; }
    }
}
