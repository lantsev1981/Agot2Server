using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace GameService
{
    // ПРИМЕЧАНИЕ. Команду "Переименовать" в меню "Рефакторинг" можно использовать для одновременного изменения имени интерфейса "IService1" в коде и файле конфигурации.
    [ServiceContract]
    public interface IGameService
    {
        [OperationContract]
        WCFGame Connect(string login, string gamePassword, string homeType);

        [OperationContract]
        bool? CheckBlackList(string login);

        [OperationContract]
        bool CheckAccessHome(string login, string homeType);

        [OperationContract]
        List<WCFStep> GetStep(WCFGameUser clientUser, int stepIndex = 0);

        [OperationContract]
        List<WCFGameUser> GetUserInfo(WCFGameUser clientUser);

        [OperationContract(IsOneWay = true)]
        void DisConnect(WCFGameUser clientUser);

        [OperationContract(IsOneWay = true)]
        void SendStep(WCFStep wcfStep);
    }
}
