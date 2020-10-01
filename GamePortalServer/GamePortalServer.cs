using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.ServiceModel;

namespace GamePortal
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public partial class GamePortalServer : IGamePortalServer
    {
        /*public AuthorizeResult VKAuthorize(user user, string password)
        {
            AuthorizeResult result = new AuthorizeResult() { Error = "" };
            try
            {
                if (user == null
                    || string.IsNullOrWhiteSpace(user.uid)
                    || string.IsNullOrWhiteSpace(user.clientId))
                    return new AuthorizeResult() { Error = "Profile is empty" };

                using (GamePortalEntities dbContext = new GamePortalEntities())
                {
                    ApiUser apiUser = dbContext.ApiUsers.SingleOrDefault(p1 => p1.uid == user.uid && p1.isFacebook == user.isFacebook);
                    User gpUser = apiUser?.User;

                    //регистрируем пользоваеля
                    if (gpUser == null)
                    {
                        gpUser = new User() { Login = Guid.NewGuid().ToString() };
                        dbContext.Users.Add(gpUser);

                        apiUser = new ApiUser(user);
                        gpUser.ApiUsers.Add(apiUser);
                    }


                    //Если пароль устанавливается впервые
                    if (string.IsNullOrWhiteSpace(gpUser.Password))
                    {
                        if (!string.IsNullOrWhiteSpace(password))
                            gpUser.Password = password;
                        else
                            result.Error += "Error! Password is not set. Set password. ";
                    }

                    //Если устанавливается новый email
                    if (!string.IsNullOrWhiteSpace(user.email))
                    {
                        if (gpUser.Password == password)
                        {
                            if (apiUser.email != user.email)
                            {
                                apiUser.emailConfirm = false;
                                apiUser.email = user.email;
                            }
                        }
                        else
                            result.Error += "Error set email! Password wrong. ";
                    }

                    //Если email не подтвержён
                    if (string.IsNullOrWhiteSpace(apiUser.email))
                        result.Error += "Error! Email is empty. Set your email. ";
                    else if (!apiUser.email.Contains('@'))
                        result.Error += "Error! Email is not correct. Check your email. ";
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
                            result.Error += $"Denied! Confirmation link not send! Try again in {15 - (int)(dateTimeNow - apiUser.resetPswDate.Value).TotalMinutes} minutes. ";
                    }

                    //Если изменился ключ доступа
                    if (apiUser.ClientId != user.clientId)
                    {
                        if (gpUser.Password == password)
                        {
                            apiUser.Sync(user);
                            gpUser.Version = Guid.NewGuid();
                        }
                        else
                            result.Error += "Error update profile! Password wrong. ";
                    }
                    else
                    {
                        apiUser.Sync(user);
                        gpUser.Version = Guid.NewGuid();
                    }

                    dbContext.SaveChanges();

                    if (apiUser.ClientId == user.clientId)
                        result.Email = apiUser.email;

                    if (string.IsNullOrWhiteSpace(result.Error))
                        result.Login = gpUser.Login;

                    return result;
                }
            }
            catch (Exception exp)
            {
                result.Error += exp.Message;
                return result;
            }
        }*/

        public static TimeSpan DataLiveTime = TimeSpan.FromDays(90);
        public List<ProfileVersion> GetProfilesVersion()
        {
            try
            {
                using (GamePortalEntities dbContext = new GamePortalEntities())
                {
                    DateTimeOffset time = DateTimeOffset.UtcNow - GamePortalServer.DataLiveTime;
                    List<ProfileVersion> result = dbContext.Users.Where(p => p.ApiUsers.Max(p1 => p1.LastConnection) > time)
                        .Select(p => new ProfileVersion() { Login = p.Login, Version = p.Version })
                        .ToList();

#if DEBUG
                    /*var xml = new PublicFileJson<List<ProfileVersion>>("GetProfilesVersion.txt");
                    xml.Value = result;
                    xml.Write();*/
#endif

                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        public WCFUser GetProfileByLogin(string login)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login))
                    return null;

                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    User gpUser = gamePortal.Users.FirstOrDefault(p => p.Login == login);
                    if (gpUser == null)
                    {
                        return null;
                    }

                    WCFUser result = gpUser.ToWCFUser(gamePortal);

#if DEBUG
                    /*var xml = new PublicFileJson<WCFUser>("GetProfileByLogin.txt");
                    xml.Value = result;
                    xml.Write();*/
#endif

                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        public void PassRate(string login, Guid id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login) || id == Guid.Empty)
                    return;

                using (GamePortalEntities dbContext = new GamePortalEntities())
                {
                    User gpUser = dbContext.Users.FirstOrDefault(p => p.Login == login);
                    if (gpUser == null)
                        return;

                    UserGame userGame = gpUser.UserGames.FirstOrDefault(p => p.Id == id);
                    if (userGame == null)
                        return;

                    userGame.IsIgnoreMind = false;
                    gpUser.Version = Guid.NewGuid();
                    dbContext.SaveChanges();
                }
            }
            catch
            {
                return;
            }
        }

        public void LikeRate(string login, string likeLogin, bool? isLike)
        {
            if (string.IsNullOrWhiteSpace(login)
                || string.IsNullOrWhiteSpace(likeLogin)
                || login == likeLogin)
                return;

            try
            {
                using (GamePortalEntities dbContext = new GamePortalEntities())
                {
                    User user = dbContext.Users.FirstOrDefault(p => p.Login == login);
                    if (user == null)
                        return;

                    User likeUser = dbContext.Users.FirstOrDefault(p => p.Login == likeLogin);
                    if (likeLogin == null)
                        return;

                    UserLike userLike = likeUser.UserLikes.FirstOrDefault(p => p.LikeLogin == user.Login);
                    if (userLike != null)
                        likeUser.UserLikes.Remove(userLike);

                    if (isLike.HasValue)
                    {
                        likeUser.UserLikes.Add(new UserLike()
                        {
                            LikeLogin = user.Login,
                            IsLike = isLike.Value,
                            Date = DateTimeOffset.UtcNow
                        });
                    }

                    //user.Version = Guid.NewGuid();
                    likeUser.Version = Guid.NewGuid();
                    dbContext.SaveChanges();
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// редактирует чёрнобелый список пользователя
        /// </summary>
        /// <param name="login"></param>
        /// <param name="specialLogin"></param>
        /// <param name="isBlock"></param>
        public bool SpecialUser(string login, string specialLogin, bool? isBlock)
        {
            try
            {
                //проверка наличия ключа доступа и входных данных
                if (string.IsNullOrWhiteSpace(login)
                    || string.IsNullOrWhiteSpace(specialLogin)
                    || (login == specialLogin && isBlock == true))
                    return false;

                using (GamePortalEntities dbContext = new GamePortalEntities())
                {
                    User user = dbContext.Users.FirstOrDefault(p => p.Login == login);
                    if (user == null || (isBlock == false && user.AllPower < 200) || (isBlock == true && user.AllPower < 300))
                        return false;

                    //удаление старой записи
                    SpecialUser value = user.SpecialUsers.FirstOrDefault(p => p.SpecialLogin == specialLogin);
                    if (value != null)
                        user.SpecialUsers.Remove(value);

                    //добавление новой
                    if (isBlock != null)
                        user.SpecialUsers.Add(new SpecialUser() { SpecialLogin = specialLogin, IsBlock = isBlock.Value });

                    user.Version = Guid.NewGuid();
                    dbContext.SaveChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static Func<WCFUser, string, string, bool> UserInviteFunc;
        public bool? InviteUser(string login, string inviteLogin, string msg)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login)
                    || string.IsNullOrWhiteSpace(inviteLogin)
                    || string.IsNullOrWhiteSpace(msg))
                    return false;

                //система уведомлений не подключена
                if (UserInviteFunc == null)
                    return false;

                //только онлайн пользователей
                //if (!_OnlineUsers.Any(p => p.Login == inviteLogin))
                //    return false;
                if (!_OnlineUsers.TryGetValue(inviteLogin, out WCFUser inviteUser))
                    return false;

                using (GamePortalEntities dbContext = new GamePortalEntities())
                {
                    User user = dbContext.Users.FirstOrDefault(p => p.Login == login);
                    if (user == null || (user.AllPower < 200))
                        return null;

                    bool result = UserInviteFunc(user.ToWCFUser(dbContext), inviteLogin, msg);

                    return result;
                }
            }
            catch
            {
                return false;
            }
        }

        public List<string> GetLikeProfile(string login)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login))
                    return null;

                using (GamePortalEntities dbContext = new GamePortalEntities())
                {
                    User user = dbContext.Users.FirstOrDefault(p => p.Login == login);
                    if (user == null || (user.AllPower < 100))
                        return null;

                    List<string> clientIdList = user.ApiUsers.Select(p => p.ClientId).ToList();
                    return dbContext.Users.Where(p => p.Login != user.Login
                        && !string.IsNullOrEmpty(p.Password)
                        && !p.UserGames.Any(p1 => !p1.EndTime.HasValue)
                        && p.ApiUsers.Any(p1 => clientIdList.Any(p2 => p2 == p1.ClientId)))
                        .Select(p => p.Login).ToList();
                }
            }
            catch
            {
                return null;
            }
        }

        public static Action<string, string> ChangeGameWhenLinkAccounts;
        public bool LinkAccounts(string login, string linkLogin, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login)
                    || string.IsNullOrWhiteSpace(linkLogin)
                    || string.IsNullOrWhiteSpace(password)
                    || login == linkLogin)
                    return false;

                using (GamePortalEntities dbContext = new GamePortalEntities())
                {
                    User user = dbContext.Users.FirstOrDefault(p => p.Login == login);
                    if (user == null || (user.AllPower < 100))
                        return false;

                    List<string> clientIdList = user.ApiUsers.Select(p => p.ClientId).ToList();
                    User linkUser = dbContext.Users.FirstOrDefault(p => p.Login == linkLogin);
                    if (linkUser == null || string.IsNullOrEmpty(linkUser.Password) || linkUser.Password != password
                        || linkUser.UserGames.Any(p1 => !p1.EndTime.HasValue)
                        || !linkUser.ApiUsers.Any(p1 => clientIdList.Any(p2 => p2 == p1.ClientId)))
                        return false;

                    linkUser.ApiUsers.ToList().ForEach(p => user.ApiUsers.Add(new ApiUser()
                    {
                        first_name = p.first_name,
                        last_name = p.last_name,
                        email = p.email,
                        photo = p.photo,
                        uid = p.uid,
                        isFacebook = p.isFacebook,
                        ClientId = p.ClientId,
                        LastConnection = p.LastConnection,
                        emailConfirm = p.emailConfirm
                    }));

                    linkUser.Titles.ToList().ForEach(p => user.Titles.Add(new Title()
                    {
                        Id = p.Id,
                        Name = p.Name
                    }));

                    linkUser.Payments.ToList().ForEach(p => user.Payments.Add(new Payment()
                    {
                        Id = p.Id,
                        Time = p.Time,
                        Event = p.Event,
                        Power = p.Power,
                        Comment = p.Comment,
                        IsPublic = p.IsPublic
                    }));

                    linkUser.UserGames.ToList().ForEach(p =>
                    {
                        if (user.UserGames.All(p1 => p1.GameId != p.GameId))
                        {
                            user.UserGames.Add(
                                new UserGame()
                                {
                                    Id = p.Id,
                                    GameId = p.GameId,
                                    GameType = p.GameType,
                                    StartTime = p.StartTime,
                                    EndTime = p.EndTime,
                                    HomeType = p.HomeType,
                                    HonorPosition = p.HonorPosition,
                                    MindPosition = p.MindPosition,
                                    IsIgnoreHonor = p.IsIgnoreHonor,
                                    IsIgnoreMind = p.IsIgnoreMind
                                });
                        }
                    });

                    //кто лайкнул меня
                    linkUser.UserLikes.ToList().ForEach(p =>
                    {
                        if (!user.UserLikes.Any(p1 => p.LikeLogin == p1.LikeLogin))
                        {
                            user.UserLikes.Add(new UserLike()
                            {
                                LikeLogin = p.LikeLogin,
                                Date = p.Date,
                                IsLike = p.IsLike
                            });
                        }
                    });

                    //Кому я поставил лайки
                    dbContext.UserLikes.Where(p => p.LikeLogin == linkUser.Login).ToList().ForEach(p =>
                    {
                        if (!p.User.UserLikes.Any(p1 => p1.LikeLogin == user.Login))
                        {
                            dbContext.UserLikes.Add(new UserLike()
                            {
                                Login = p.Login,
                                LikeLogin = user.Login,
                                IsLike = p.IsLike,
                                Date = p.Date
                            });
                        }

                        p.User.Version = Guid.NewGuid();
                        dbContext.UserLikes.Remove(p);
                    });

                    //кто у меня в списке
                    linkUser.SpecialUsers.ToList().ForEach(p =>
                    {
                        if (!user.SpecialUsers.Any(p1 => p.SpecialLogin == p1.SpecialLogin))
                        {
                            user.SpecialUsers.Add(new SpecialUser()
                            {
                                SpecialLogin = p.SpecialLogin,
                                IsBlock = p.IsBlock
                            });
                        }
                    });

                    //Кто меня добавил в исключения
                    dbContext.SpecialUsers.Where(p => p.SpecialLogin == linkUser.Login).ToList().ForEach(p =>
                    {
                        if (!p.User.SpecialUsers.Any(p1 => p1.SpecialLogin == user.Login))
                        {
                            dbContext.SpecialUsers.Add(new SpecialUser()
                            {
                                Login = p.Login,
                                SpecialLogin = user.Login,
                                IsBlock = p.IsBlock
                            });
                        }

                        p.User.Version = Guid.NewGuid();
                        dbContext.SpecialUsers.Remove(p);
                    });

                    user.Version = Guid.NewGuid();
                    dbContext.Users.Remove(linkUser);
                    dbContext.SaveChanges();

                    //Заменяем участи в играх
                    ChangeGameWhenLinkAccounts?.Invoke(login, linkLogin);

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool ClearProfile(string login)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login))
                    return false;

                using (GamePortalEntities dbContext = new GamePortalEntities())
                {
                    //проверка ключа доступа
                    User user = dbContext.Users.FirstOrDefault(p => p.Login == login);
                    if (user == null || (user.AllPower < 100))
                    {
                        return false;
                    }

                    DateTimeOffset time = DateTimeOffset.UtcNow - GamePortalServer.DataLiveTime;
                    user.UserGames.Where(p => p.EndTime.HasValue).ToList().ForEach(p =>
                    {
                        p.StartTime = time;
                    });

                    dbContext.UserLikes.RemoveRange(user.UserLikes);

                    user.Version = Guid.NewGuid();
                    dbContext.SaveChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
