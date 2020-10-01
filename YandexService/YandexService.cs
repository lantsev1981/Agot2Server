using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace YandexService
{
    // ПРИМЕЧАНИЕ. Команду "Переименовать" в меню "Рефакторинг" можно использовать для одновременного изменения имени класса "Service1" в коде и файле конфигурации.
    public class YandexService : IYandexService
    {
        ServiceHost serviceHost;

        public void Start()
        {
            serviceHost = new ServiceHost(typeof(YandexService));
            serviceHost.Open();
        }

        public void Notify(string value)
        {

        }
    }
}
