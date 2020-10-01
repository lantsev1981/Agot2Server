using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace PayPal
{
    // ПРИМЕЧАНИЕ. Команду "Переименовать" в меню "Рефакторинг" можно использовать для одновременного изменения имени интерфейса "IService1" в коде и файле конфигурации.
    [ServiceContract]
    public interface IPayPalService
    {
        [OperationContract]
        [WebGet(UriTemplate = "/?txn_id={operationId}&custom={login}&memo={comment}&option_selection1={isPublic}&mc_gross={gross}&mc_currency={currency}")]
        void Notify(string operationId, string login, string comment, string isPublic, string gross, string currency);
    }
}
