using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Threading.Tasks;

namespace GamePortal
{
    [ServiceContract]
    public interface IWebService
    {
        [OperationContract]
        [WebGet(UriTemplate = "GetProfile/?uid={uid}")]
        Task<Message> GetProfile(string uid);

        /// <summary>
        /// Получает счётчик онлайн игроков
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [WebGet(UriTemplate = "GetOnlineCounters/")]
        Task<Message> GetOnlineCounters();
    }
}
