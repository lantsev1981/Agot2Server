using MyLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace GamePortal
{
    [ServiceContract]
    public interface IWebService
    {
        [OperationContract]
        [WebGet(UriTemplate = "GetProfile/?uid={uid}&gameId={gameId}")]
        Message GetProfile(string uid, string gameId);

        /// <summary>
        /// Получает счётчик онлайн игроков
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [WebGet(UriTemplate = "GetOnlineCounters/")]
        Message GetOnlineCounters();

        [OperationContract]
        [WebGet(UriTemplate = "GetCurrentProfit/")]
        Message GetCurrentProfit();
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class WebService : IWebService, IDisposable
    {
        #region service host
        private ServiceHost serviceHost;

        public void Start()
        {
            serviceHost = new ServiceHost(this);
            serviceHost.Open();
        }

        public void Dispose()
        {
            serviceHost.Abort();
        }
        #endregion

        #region service interface
        public Message GetProfile(string uid, string gameId)
        {
            //try
            //{
            if (string.IsNullOrEmpty(uid) && string.IsNullOrEmpty(gameId))
                return null;

            WebOperationContext current = WebOperationContext.Current;
            //return await GamePortalServer.TaskFactory.StartNew<Message>(() =>
            //{
            try
            {
                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    ApiUser user = uid == null ? gamePortal.ApiUsers.FirstOrDefault(p => p.Login == gameId) : gamePortal.ApiUsers.FirstOrDefault(p => p.uid == uid);
                    if (user == null)
                        return null;

                    Profile result = new Profile
                    {
                        Id = user.Login,
                        FIO = $"{user.first_name} {user.last_name}",
                        AllPower = user.User.AllPower
                    };
                    result.Titles.AddRange(user.User.Titles.Select(p => p.Name));

                    return ExtHttp.GetJsonStream(result, current);
                }
            }
            catch
            {
                return null;
            }
            //    });
            //}
            //catch
            //{
            //    return null;
            //}
        }

        public Message GetOnlineCounters()
        {
            //try
            //{
            WebOperationContext current = WebOperationContext.Current;
            //return await GamePortalServer.TaskFactory.StartNew<Message>(() =>
            //{
            try
            {
                using (GamePortalEntities db = new GamePortalEntities())
                {
                    List<OnlineCounter> items = db.OnlineCounters.OrderBy(p => p.dateTime).ToList();
                    OnlineCounter maxCounter = items.Single((p) => p.id == Guid.Empty);
                    items.Remove(maxCounter);

                    OnlineCounterModel result = new OnlineCounterModel
                    {
                        Items = items.Select(p => new OnlineCounterItemModel() { DateTime = p.dateTime, Count = p.count }).ToList(),
                        MaxItem = new OnlineCounterItemModel() { DateTime = maxCounter.dateTime, Count = maxCounter.count }
                    };
                    return ExtHttp.GetJsonStream(result, current);
                }
            }
            catch
            {
                return null;
            }
            //    });
            //}
            //catch
            //{
            //    return null;
            //}
        }

        private static readonly float ProjectPrice = 500000;
        public Message GetCurrentProfit()
        {
            //try
            //{
            WebOperationContext current = WebOperationContext.Current;
            //return await GamePortalServer.TaskFactory.StartNew<Message>(() =>
            //{
            try
            {
                using (GamePortalEntities db = new GamePortalEntities())
                {
                    int total = db.Payments.Sum(p => p.Power);
                    float result = total / ProjectPrice;
                    return ExtHttp.GetJsonStream(result.ToString("P"), current);
                }
            }
            catch
            {
                return null;
            }
            //    });
            //}
            //catch
            //{
            //    return null;
            //}
        }
        #endregion
    }
}
