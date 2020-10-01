using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

using System.ServiceModel.Channels;

namespace Notifications
{
    // ПРИМЕЧАНИЕ. Команду "Переименовать" в меню "Рефакторинг" можно использовать для одновременного изменения имени класса "NotifiService" в коде и файле конфигурации.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public partial class NotifiServer : INotifiServer
    {
        //Способ получения ip клиента
        /*public string ClientIp()
        {
            OperationContext context = OperationContext.Current;
            MessageProperties prop = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty endpoint = prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            //получаем IP клиента.
            return endpoint.Address;
        }*/

        static TimeSpan _UserLiveTime = TimeSpan.FromMinutes(1);

        //LockFunc _ClientList
        public NotifiResult GetGameList(string clientVersion, string clientId)
        {
            try
            {
                if (string.IsNullOrEmpty(clientVersion) || string.IsNullOrEmpty(clientId))
                    return null;

#if !DEBUG //Проверка версии клиента
                if (!string.IsNullOrEmpty(_UpdaterHost.ClientVersion) && _UpdaterHost.ClientVersion != clientVersion)
                    return null;
#endif
                NotifiResult result = null;
                Client client;
                lock (_ClientList)
                {
                    client = _ClientList.SingleOrDefault(p => p.Login == clientId);
                    if (client == null)
                    {
                        //регистрируем новый клиент и отправляем текущие игры
                        client = new Client(clientId);
                        _ClientList.Add(client);

                        //загружаем текущие игры
                        if (NewClient != null)
                            client.NotifiList.AddRange(NewClient(clientId).Select(p => new Notifi() { Game = p }));

                        //загружаем рекламу
                        client.NotifiList.AddRange(_AdNotifiList.Value);
                    }

                    client.LastConnection = DateTimeOffset.UtcNow;
                    var time = DateTimeOffset.UtcNow - NotifiServer._UserLiveTime;
                    _ClientList.RemoveAll(p => p.LastConnection < time);
                }

                result = new NotifiResult();
                result.ClientCount = AddOnlineUserFunc(clientId);//Нельзя добавлять в область lock (_ClientList) 


                if (client.NotifiList.Count > 0)
                {
                    lock (_ClientList)
                    {
                        result.NotifiList = client.NotifiList.ToList();
                        client.NotifiList.Clear();
                    }
                }

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
