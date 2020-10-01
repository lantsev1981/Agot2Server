using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace GameService
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        void SendException(WCFGameException wcfGameException);

        [OperationContract]
        WCFGame NewGame(string clientVersion, WCFGameSettings gameSettings, string gamePassword);

        [OperationContract]
        WCFService GetGame(string clientVersion, string login);

        [OperationContract(IsOneWay = true)]
        void UpdateGamePoint(string clientVersion, string login, WCFGamePoint point);

        [OperationContract(IsOneWay = true)]
        void DisableNewGame(string clientVersion, string login, bool disable);
    }

    [DataContract]
    public partial class WCFService
    {
        [DataMember]
        public List<WCFGame> Games { get; set; }

        [DataMember]
        public bool IsDisableNewGame { get; set; }
    }
}
