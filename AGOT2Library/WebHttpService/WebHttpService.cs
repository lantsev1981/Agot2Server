using GamePortal;
using MyLibrary;
using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Threading.Tasks;

namespace Agot2Server
{
    [ServiceContract]
    public interface IWebHttpService
    {
        [OperationContract]
        [WebGet(UriTemplate = "GetWebSocket/?name={name}&game={game}")]
        Message GetWebSocket(string name, Guid game);

        [OperationContract]
        [WebGet(UriTemplate = "TryResetPasswordByUid/?uid={uid}&isFacebook={isFacebook}")]
        Message TryResetPasswordByUid(string uid, bool isFacebook);

        [OperationContract]
        [WebGet(UriTemplate = "TryResetPasswordByEmail/?email={email}")]
        Message TryResetPasswordByEmail(string email);

        [OperationContract]
        [WebGet(UriTemplate = "ResetPassword/?code={code}")]
        Message ResetPassword(string code);

        [OperationContract]
        [WebGet(UriTemplate = "ChangeEmail/?code={code}")]
        Message ChangeEmail(string code);

        [OperationContract]
        [WebGet(UriTemplate = "GetLogin/?email={email}&password={password}&clientId={clientId}")]
        Message GetLogin(string email, string password, string clientId);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public partial class WebHttpService : IWebHttpService, IDisposable
    {
        private WebServiceHost _ServiceHost;
        public WebHttpService()
        {
            _ServiceHost = new WebServiceHost(this);
            _ServiceHost.Open();
        }

        public void Dispose()
        {
            _ServiceHost.Abort();
        }

        public Message GetWebSocket(string name, Guid game)
        {
            try
            {
                WebOperationContext current = WebOperationContext.Current;
                lock (Service.WebSockServiceList)
                {
                    WebSocketModel result = Service.WebSockServiceList.SingleOrDefault(p => p.Name == name && p.GameId == game);

                    if (result == null)
                    {
                        return null;
                    }

                    return ExtHttp.GetJsonStream(result.Port, current);
                }
            }
            catch (Exception exp)
            {
                GameService.GameException.NewGameException(game, "GetWebSocket", exp, false);
                return null;
            }
        }

        public Message TryResetPasswordByUid(string uid, bool isFacebook)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(uid))
                {
                    return null;
                }

                WebOperationContext current = WebOperationContext.Current;
                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    string result = "";
                    ApiUser user = gamePortal.ApiUsers.FirstOrDefault(p => p.uid == uid && p.isFacebook == isFacebook);
                    if (user == null || string.IsNullOrWhiteSpace(user.email) || !user.email.Contains('@'))
                    {
                        result = "Error! User or his email not found.";
                    }
                    else
                    {
                        DateTimeOffset dateTimeNow = DateTimeOffset.UtcNow;
                        if (user.resetPswDate == null || (dateTimeNow - user.resetPswDate.Value).TotalMinutes > 15)
                        {
                            try
                            {
                                user.resetPswCode = Guid.NewGuid().ToString();
                                user.resetPswDate = dateTimeNow;
                                MyLibrary.EMail.Send(user.email, "AGOT: online", $"<a href='http://lantsev1981.ru:6999/WebHttpService/ResetPassword/?code={user.resetPswCode}'>Click the link to reset the password.</a>", null, true);
                                result = $"Attention! A link to reset password was send to address '{user.email.Substring(0, 5)}...'. Click the link in your email. ";
                            }
                            catch
                            {
                                user.resetPswCode = null;
                                user.resetPswDate = dateTimeNow;
                                result = "Error! Confirmation link not send! Try again in 15 minutes.";
                            }
                            gamePortal.SaveChanges();
                        }
                        else
                        {
                            result = $" Denied! Confirmation link not send!Try again in {15 - (int)(dateTimeNow - user.resetPswDate.Value).TotalMinutes} minutes.";
                        }
                    }

                    return ExtHttp.GetJsonStream(result, current);
                }
            }
            catch (Exception exp)
            {
                GameService.GameException.NewGameException(null, "TryResetPasswordByUid", exp, false);
                return null;
            }
        }

        public Message TryResetPasswordByEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return null;
                }

                WebOperationContext current = WebOperationContext.Current;
                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    string result = "";
                    ApiUser user = gamePortal.ApiUsers.FirstOrDefault(p => p.email == email);
                    if (user == null)
                    {
                        result = "Error! User not found.";
                    }
                    else
                    {
                        DateTimeOffset dateTimeNow = DateTimeOffset.UtcNow;
                        if (user.resetPswDate == null || (dateTimeNow - user.resetPswDate.Value).TotalMinutes > 15)
                        {
                            try
                            {
                                user.resetPswCode = Guid.NewGuid().ToString();
                                user.resetPswDate = dateTimeNow;
                                MyLibrary.EMail.Send(user.email, "AGOT: online", $"<a href='http://lantsev1981.ru:6999/WebHttpService/ResetPassword/?code={user.resetPswCode}'>Click the link to reset the password.</a>", null, true);
                                result = $"Attention! A link to reset password was send to address '{user.email.Substring(0, 5)}...'. Click the link in your email. ";
                            }
                            catch (Exception e)
                            {
                                user.resetPswCode = null;
                                user.resetPswDate = dateTimeNow;
                                result = "Error! Confirmation link not send! Try again in 15 minutes.";
                            }
                            gamePortal.SaveChanges();
                        }
                        else
                        {
                            result = $"Denied! Confirmation link not send! Try again in {15 - (int)(dateTimeNow - user.resetPswDate.Value).TotalMinutes} minutes.";
                        }
                    }

                    return ExtHttp.GetJsonStream(result, current);
                }
            }
            catch (Exception exp)
            {
                GameService.GameException.NewGameException(null, "TryResetPasswordByEmail", exp, false);
                return null;
            }
        }

        public Message ResetPassword(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return null;
                }

                WebOperationContext current = WebOperationContext.Current;
                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    string result = "";
                    ApiUser user = gamePortal.ApiUsers.FirstOrDefault(p => p.resetPswCode == code);
                    if (user == null)
                    {
                        result = "Denied! The link is not valid.";
                    }
                    else
                    {
                        user.User.Password = null;
                        user.resetPswCode = null;
                        user.resetPswDate = null;
                        user.emailConfirm = true;
                        gamePortal.SaveChanges();
                        result = "Email confirmed. Password was reset.";
                    }

                    return ExtHttp.GetJsonStream(result, current);
                }
            }
            catch (Exception exp)
            {
                GameService.GameException.NewGameException(null, "ResetPassword", exp, false);
                return null;
            }
        }

        public Message ChangeEmail(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return null;
                }

                WebOperationContext current = WebOperationContext.Current;
                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    string result = "";
                    ApiUser user = gamePortal.ApiUsers.FirstOrDefault(p => p.resetPswCode == code);
                    if (user == null)
                    {
                        result = "Denied! The link is not valid.";
                    }
                    else
                    {
                        user.emailConfirm = true;
                        user.resetPswCode = null;
                        user.resetPswDate = null;
                        gamePortal.SaveChanges();
                        result = "Email confirmed.";
                    }

                    return ExtHttp.GetJsonStream(result, current);
                }
            }
            catch (Exception exp)
            {
                GameService.GameException.NewGameException(null, "ChangeEmail", exp, false);
                return null;
            }
        }

        public Message GetLogin(string email, string password, string clientId)
        {
            try
            {
                WebOperationContext current = WebOperationContext.Current;
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(clientId))
                {
                    return ExtHttp.GetJsonStream(new AuthorizeResult { Error = "Denied! Inputs wrong " }, current);
                }

                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    AuthorizeResult result = new AuthorizeResult();
                    ApiUser apiUser = gamePortal.ApiUsers.FirstOrDefault(p => p.email == email);
                    User gpUser;

                    //регистрируем пользоваеля
                    if (apiUser == null)
                    {
                        gpUser = new User() { Login = Guid.NewGuid().ToString() };
                        gamePortal.Users.Add(gpUser);

                        apiUser = new ApiUser() { uid = gpUser.Login, first_name = email.Split('@')[0], last_name = "", photo = "", ClientId = clientId };
                        gpUser.ApiUsers.Add(apiUser);
                    }
                    else
                    {
                        gpUser = apiUser.User;
                    }


                    //Если пароль устанавливается впервые
                    if (string.IsNullOrWhiteSpace(gpUser.Password))
                    {
                        if (!string.IsNullOrWhiteSpace(password))
                        {
                            gpUser.Password = password;
                        }
                        else
                        {
                            result.Error += "Error! Password is not set. Set password. ";
                        }
                    }

                    //Если устанавливается новый email
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        if (apiUser.email != email)
                        {
                            if (gpUser.Password == password)
                            {
                                apiUser.emailConfirm = false;
                                apiUser.email = email;
                            }
                            else
                            {
                                result.Error += "Error set email! Password wrong. ";
                            }
                        }
                    }

                    //Если email не подтвержён
                    if (string.IsNullOrWhiteSpace(apiUser.email))
                    {
                        result.Error += "Error! Email is empty. Set your email. ";
                    }
                    else if (!apiUser.email.Contains('@'))
                    {
                        result.Error += "Error! Email is not correct. Check your email. ";
                    }
                    else if (!apiUser.emailConfirm)
                    {
                        result.Error += "Error! Email is not confirmed. ";

                        DateTimeOffset dateTimeNow = DateTimeOffset.UtcNow;
                        if (apiUser.resetPswDate == null || (dateTimeNow - apiUser.resetPswDate.Value).TotalMinutes > 15)
                        {
                            try
                            {
                                apiUser.resetPswCode = Guid.NewGuid().ToString();
                                apiUser.resetPswDate = dateTimeNow;
                                MyLibrary.EMail.Send(apiUser.email, "AGOT: online", $"<a href='http://lantsev1981.ru:6999/WebHttpService/ChangeEmail/?code={apiUser.resetPswCode}'>Click the link to confirm the email.</a>", null, true);
                                result.Error += $"Attention! Confirmation link was send to address '{apiUser.email.Substring(0, 5)}...'. Click the link in your email. ";
                            }
                            catch
                            {
                                apiUser.resetPswCode = null;
                                apiUser.resetPswDate = dateTimeNow;
                                result.Error += "Error! Confirmation link not send! Try again in 15 minutes. ";
                            }
                        }
                        else
                        {
                            result.Error += $"Denied! Confirmation link not send! Try again in {15 - (int)(dateTimeNow - apiUser.resetPswDate.Value).TotalMinutes} minutes. ";
                        }
                    }

                    //Если изменился ключ доступа
                    if (apiUser.ClientId != clientId)
                    {
                        if (gpUser.Password == password)
                        {
                            apiUser.ClientId = clientId;
                            apiUser.LastConnection = DateTimeOffset.UtcNow;
                            gpUser.Version = Guid.NewGuid();
                        }
                        else
                        {
                            result.Error += "Error update profile! Password wrong. ";
                        }
                    }
                    else
                    {
                        apiUser.LastConnection = DateTimeOffset.UtcNow;
                        gpUser.Version = Guid.NewGuid();
                    }

                    gamePortal.SaveChanges();

                    if (apiUser.ClientId == clientId)
                    {
                        result.Email = apiUser.email;
                    }

                    if (string.IsNullOrWhiteSpace(result.Error))
                    {
                        result.Login = gpUser.Login;
                    }

                    return ExtHttp.GetJsonStream(result, current);
                }
            }
            catch (Exception exp)
            {
                GameService.GameException.NewGameException(null, "GetLogin", exp, false);
                return null;
            }
        }

        //private class Result
        //{
        //    public string msg;
        //    public string res;
        //}
    }
}
