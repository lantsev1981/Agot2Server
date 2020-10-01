using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

using GameService;
using Updater;
using System.Drawing;
using System.IO;
//using MainSettings;
using MyLibrary;
using GamePortal;

namespace Notifications
{
    public partial class NotifiServer : IDisposable
    {
        static public Func<string, int> AddOnlineUserFunc;

        /// <summary>
        /// сообщает о новом пользователе, ожидает список доступных игр
        /// </summary>
        public event Func<string, List<WCFGame>> NewClient;

        AdNorifiList _AdNotifiList = new AdNorifiList("ad");
        List<Client> _ClientList = new List<Client>();
        ServiceHost _ServiceHost;

        UpdaterServer _UpdaterHost = new UpdaterServer("NotifiClientUpdaterSettings", true);


        public void Start()
        {
            _UpdaterHost.Start();
            _ServiceHost = new ServiceHost(this);
            _ServiceHost.Open();
        }

        //LockFunc _ClientList
        public void AddGameNotifi(WCFGame newItem)
        {
            lock (_ClientList)
            {
                _ClientList.ForEach(p => p.NotifiList.Add(new Notifi() { Game = newItem }));
            }
        }

        //LockFunc _ClientList
        public void AddUserNotifi(WCFUser user, string message)
        {
            //TODO можно загружать при старте хоста
            PublicFileJson<NotifiSettings> notifiSettings = new PublicFileJson<NotifiSettings>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs\\user.config"));
            notifiSettings.Read();
            if (notifiSettings.Value == null)
                if(!tmpMethod(notifiSettings))
                    return;

            foreach (var login in user.SignerUsers)
            {
                lock (_ClientList)
                {
                    var client = _ClientList.SingleOrDefault(p => p.Login == login);
                    if (client == null)
                        continue;

                    client.NotifiList.Add(new Notifi() { Content = message, User = user.Login, Settings = notifiSettings.Value });
                }
            }
        }

        private static bool tmpMethod(PublicFileJson<NotifiSettings> settings)
        {
            settings.Value = new NotifiSettings();
            return settings.Write();
        }

        //LockFunc _ClientList
        public bool InviteUser(WCFUser user, string inviteLogin, string msg)
        {
            lock (_ClientList)
            {
                //проверка подключена ли система оповещения пользователя
                var client = _ClientList.SingleOrDefault(p => p.Login == inviteLogin);
                if (client == null)
                    return false;

                //загрузка настроект окна приглошения
                PublicFileJson<NotifiSettings> notifiSettings = new PublicFileJson<NotifiSettings>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs\\invite.config"));
                notifiSettings.Read();
                if (notifiSettings.Value == null)
                    if(!tmpMethod(notifiSettings))
                        return false; 

                client.NotifiList.Add(new Notifi() { Content = string.Format("dynamic_message*{0}", msg), User = user.Login, Settings = notifiSettings.Value });
            }

            return true;
        }

        public void Dispose()
        {
            _ServiceHost.Abort();
            _UpdaterHost.Dispose();
        }
    }

    class Client
    {
        public string Login { get; private set; }
        public DateTimeOffset LastConnection { get; set; }
        public List<Notifi> NotifiList { get; private set; }

        public Client(string login)
        {
            Login = login;
            LastConnection = DateTimeOffset.UtcNow;
            NotifiList = new List<Notifi>();
        }
    }
}
