using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

using System.ServiceModel.Web;
using System.IO;

namespace Yandex
{
    // ПРИМЕЧАНИЕ. Команду "Переименовать" в меню "Рефакторинг" можно использовать для одновременного изменения имени интерфейса "IService1" в коде и файле конфигурации.
    [ServiceContract]
    public interface IYandexService
    {
        [OperationContract]
        [WebInvoke(Method = "POST",
                    //RequestFormat = WebMessageFormat.Json,
                    //ResponseFormat = WebMessageFormat.Json,
                    UriTemplate = "/")]
        void Notify(Stream data);
    }
}
