using Lantsev;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace GamePortal
{
    public partial class GamePortalServer : IDisposable
    {
        public static Action<WCFUser, string> AddUserNotifiFunc;

        public TaskFactory TaskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(1));
        private ServiceHost serviceHost;
        private WebService webService;

        public void Start()
        {
            webService = new WebService();
            webService.Start();

            serviceHost = new ServiceHost(this);
            serviceHost.Open();
        }

        #region online users
        private static TimeSpan _UserLiveTime = TimeSpan.FromMinutes(1);
        private ConcurrentDictionary<string, WCFUser> _OnlineUsers = new ConcurrentDictionary<string, WCFUser>();

        public List<string> GetOnlineUsers(string login)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(login))
                    return null;

                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    User gpUser = gamePortal.Users.FirstOrDefault(p => p.Login == login);
                    if (gpUser == null || gpUser.AllPower < 100)
                        return null;
                }

                List<string> result = _OnlineUsers.Keys.ToList();

#if DEBUG
                /*var xml = new PublicFileJson<List<string>>("GetOnlineUsers.txt");
                xml.Value = result;
                xml.Write();*/
#endif

                return result;
            }
            catch
            {
                return null;
            }
        }


        public int AddOnlineUser(string login)
        {
            try
            {
                int result = 0;
                bool isNewUser = false;

                if (!_OnlineUsers.TryGetValue(login, out WCFUser user))
                {
                    user = GetProfileByLogin(login);
                    if (user == null)
                        return 0;

                    if (_OnlineUsers.TryAdd(user.Login, user))
                    {
                        isNewUser = true;
                        AddUserNotifiFunc(user, "Online");
                    }
                }

                user.LastConnection = DateTimeOffset.UtcNow;
                result = _OnlineUsers.Count;

                if (RemoveOfflineUsers() || isNewUser)
                    SaveOnlineCounters();

                return result;
            }
            catch
            {
                return 0;
            }
        }

        private bool RemoveOfflineUsers()
        {
            DateTimeOffset time = DateTimeOffset.UtcNow - GamePortalServer._UserLiveTime;
            List<WCFUser> oldItems = _OnlineUsers.Values.Where(p => p.LastConnection < time).ToList();
            if (oldItems.Count > 0)
            {
                oldItems.ForEach((p) =>
                {
                    if (_OnlineUsers.TryRemove(p.Login, out p))
                        AddUserNotifiFunc(p, "Offline");
                });

                return true;
            }

            return false;
        }

        private static TimeSpan _CounterLiveTime = TimeSpan.FromDays(3);
        //Обновляем счётчик онлайн игроков
        private void SaveOnlineCounters()
        {
#if !DEBUG
            using (GamePortalEntities db = new GamePortalEntities())
            {
                //удаляем устаревшие данные
                DateTimeOffset time = DateTimeOffset.UtcNow - _CounterLiveTime;
                IQueryable<OnlineCounter> oldLikes = db.OnlineCounters.Where((p) => p.id != Guid.Empty && p.dateTime < time);
                db.OnlineCounters.RemoveRange(oldLikes);

                //Добавляем новое значение
                OnlineCounter newCounter = new OnlineCounter() { id = Guid.NewGuid(), dateTime = DateTimeOffset.UtcNow, count = _OnlineUsers.Count };
                db.OnlineCounters.Add(newCounter);

                //изменяем максимум
                OnlineCounter maxCounter = db.OnlineCounters.Single((p) => p.id == Guid.Empty);
                if (newCounter.count >= maxCounter.count)
                {
                    maxCounter.count = newCounter.count;
                    maxCounter.dateTime = newCounter.dateTime;
                }

                db.SaveChanges();
            }
#endif
        }
        #endregion

        public void StartUserGame(string login, string homeType, Guid gameId, int gameType, bool isIgnoreDurationHours =false, bool IsIgnoreMind = false)
        {
#if !DEBUG //не учитывать рейтинг в отладочном режиме
            StopUserGame(login, gameId);

            TaskFactory.StartNew(() =>
            {
                try
                {
                    using (GamePortalEntities gamePortal = new GamePortalEntities())
                    {
                        User gpUser = gamePortal.Users.SingleOrDefault(p => p.Login == login);
                        if (gpUser == null)
                            return;

                        UserGame userGame = gpUser.UserGames.SingleOrDefault(p => p.GameId == gameId);
                        if (userGame == null)
                        {
                            userGame = new UserGame()
                            {
                                Id = Guid.NewGuid(),
                                GameId = gameId,
                                GameType = gameType,//+1 - игра с рандомом
                                Login = login,
                                HomeType = homeType,
                                StartTime = DateTimeOffset.UtcNow,
                                User = gpUser,
                                IsIgnoreMind = IsIgnoreMind,
                                IsIgnoreDurationHours = isIgnoreDurationHours
                            };
                            gpUser.UserGames.Add(userGame);
                            gpUser.Version = Guid.NewGuid();
                            gamePortal.SaveChanges();
                        }
                    }
                }
                catch
                {
                }
            });
#endif
        }

        public void StopUserGame(string login, Guid gameId, int mindPosition = 0, bool isIgnoreHonor = false)
        {
            TaskFactory.StartNew(() =>
            {
                try
                {
                    using (GamePortalEntities gamePortal = new GamePortalEntities())
                    {
                        User gpUser = gamePortal.Users.SingleOrDefault(p => p.Login == login);
                        if (gpUser == null)
                            return;

                        UserGame userGame = gpUser.UserGames.SingleOrDefault(p => p.GameId == gameId);

                        if (userGame != null)
                        {
                            //Завершил игру
                            if (mindPosition != 0)
                            {
                                userGame.IsIgnoreHonor = false;
                                userGame.HonorPosition = 5;
                                userGame.MindPosition = mindPosition;

                                AddUserNotifiFunc?.Invoke(gpUser.ToWCFUser(gamePortal), string.Format("dynamic_gameEnd*{0}*{1}", userGame.HomeType, userGame.MindPosition));
                            }
                            else
                            {
                                //наказание ослабевает по мере увеличения их количества в партии
                                userGame.HonorPosition = 6 - gamePortal.UserGames.Count(p => p.GameId == gameId && !p.EndTime.HasValue);
                                userGame.IsIgnoreHonor = isIgnoreHonor;

                                if (AddUserNotifiFunc != null)
                                {
                                    WCFUserGame wcfUserGame = userGame.ToWCFUserGame();
                                    if (wcfUserGame.IsIgnoreHonor)
                                        AddUserNotifiFunc(gpUser.ToWCFUser(gamePortal), $"dynamic_leftGame1*{"unknown home"}");//userGame.HomeType
                                    else
                                        AddUserNotifiFunc(gpUser.ToWCFUser(gamePortal), $"dynamic_leftGame2*{"unknown home"}*0");//userGame.HomeType
                                }
                            }

                            userGame.EndTime = DateTimeOffset.UtcNow;
                            gpUser.Version = Guid.NewGuid();

                            gamePortal.SaveChanges();
                        }
                    }
                }
                catch
                {
                }
            });
        }

        public Dictionary<string, string> GetPrivateProfileData(string login)
        {
            if (string.IsNullOrEmpty(login))
            {
                return null;
            }

            using (GamePortalEntities gamePortal = new GamePortalEntities())
            {
                User user = gamePortal.Users.FirstOrDefault(p => p.Login == login);
                if (user == null)
                {
                    return null;
                }

                Dictionary<string, string> result = new Dictionary<string, string>
                {
                    { "clientId", user.LastApiUser.ClientId },
                    { "email", user.LastApiUser.email },
                    { "password", user.Password }
                };

                return result;
            }
        }

        public void YandexMoneyPayment(string[] split, int power, DateTimeOffset time, string operation_id)
        {
            using (GamePortalEntities gamePortal = new GamePortalEntities())
            {
                string login = split[0];

                User gpUser = gamePortal.Users.SingleOrDefault(p => p.Login == login);
                if (gpUser == null)
                {
                    split = new string[3] { "17a87d89-b8d7-4274-9049-78d7b6af94af", "0", string.Format("Неизвестный пользователь: {0}", string.Join("|", split)) };
                    gpUser = gamePortal.Users.Single(p => p.Login == "17a87d89-b8d7-4274-9049-78d7b6af94af");
                }

                string comment = string.IsNullOrWhiteSpace(split[2]) ? null : split[2];
                gpUser.Payments.Add(new Payment() { Event = operation_id, Id = Guid.NewGuid(), Power = power, Time = time, IsPublic = split[1] == "1", Comment = comment });
                gpUser.Version = Guid.NewGuid();
                gamePortal.SaveChanges();

                UserInviteFunc(gpUser.ToWCFUser(gamePortal), "17a87d89-b8d7-4274-9049-78d7b6af94af", string.Format("Заплатил: {0}. Коментарий: {1}", power, comment));

                if (gpUser.Login != "17a87d89-b8d7-4274-9049-78d7b6af94af")
                {
                    gpUser = gamePortal.Users.Single(p => p.Login == "17a87d89-b8d7-4274-9049-78d7b6af94af");
                    UserInviteFunc(gpUser.ToWCFUser(gamePortal), login, "dynamic_thanks");
                }
            }
        }

        public void PayPalPayment(string operationId, string login, string comment, bool isPublic, int power)
        {
            using (GamePortalEntities gamePortal = new GamePortalEntities())
            {
                if (gamePortal.Payments.Any(p => p.Event == operationId))
                {
                    return;
                }

                User gpUser = gamePortal.Users.SingleOrDefault(p => p.Login == login);
                if (gpUser == null)
                {
                    comment = string.Format("Неизвестный пользователь: {0}, comment = {1}, isPublic = {2}", login, comment, isPublic);
                    isPublic = false;
                    gpUser = gamePortal.Users.Single(p => p.Login == "17a87d89-b8d7-4274-9049-78d7b6af94af");
                }

                gpUser.Payments.Add(new Payment() { Event = operationId, Id = Guid.NewGuid(), Comment = string.IsNullOrWhiteSpace(comment) ? null : comment, IsPublic = isPublic, Power = power, Time = DateTimeOffset.UtcNow });
                gpUser.Version = Guid.NewGuid();
                gamePortal.SaveChanges();

                UserInviteFunc(gpUser.ToWCFUser(gamePortal), "17a87d89-b8d7-4274-9049-78d7b6af94af", string.Format("Заплатил: {0}. Коментарий: {1}", power, comment));

                if (gpUser.Login != "17a87d89-b8d7-4274-9049-78d7b6af94af")
                {
                    gpUser = gamePortal.Users.Single(p => p.Login == "17a87d89-b8d7-4274-9049-78d7b6af94af");
                    UserInviteFunc(gpUser.ToWCFUser(gamePortal), login, "dynamic_thanks");
                }
            }
        }

        public void Dispose()
        {
            serviceHost.Abort();
            //#if !DEBUG
            webService.Dispose();
            //#endif
        }
    }
}
