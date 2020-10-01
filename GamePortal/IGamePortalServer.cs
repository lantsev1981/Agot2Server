using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.Runtime.Serialization;

namespace GamePortal
{
    [ServiceContract]
    public interface IGamePortalServer
    {
        /// <summary>
        /// Регистрирует и авторизует пользователя по учётным данным ВК
        /// </summary>
        /*[OperationContract]
        AuthorizeResult VKAuthorize(user vkUser, string password);*/

        [OperationContract]
        List<ProfileVersion> GetProfilesVersion();
        [OperationContract]
        List<string> GetOnlineUsers(string login);

        [OperationContract]
        WCFUser GetProfileByLogin(string login);

        /// <summary>
        /// Засчитать партию в разум
        /// </summary>
        /// <param name="login"></param>
        /// <param name="id"></param>
        [OperationContract]
        void PassRate(string login, Guid id);


        [OperationContract]
        void LikeRate(string login, string likeLogin, bool? isLike);

        [OperationContract]
        bool SpecialUser(string login, string specialLogin, bool? isBlock);

        [OperationContract]
        bool? InviteUser(string login, string inviteLogin, string msg);

        [OperationContract]
        bool ClearProfile(string login);

        [OperationContract]
        List<string> GetLikeProfile(string login);

        [OperationContract]
        bool LinkAccounts(string baseLogin, string login, string password);
    }


    [DataContract]
    public class ProfileVersion
    {
        [DataMember]
        public string Login { get; set; }
        [DataMember]
        public Guid Version { get; set; }
    }

    [DataContract]
    public class AuthorizeResult
    {
        [DataMember]
        public string Login { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public string Error { get; set; }

    }
}
