using ChatServer;
using GamePortal;
using GameService;
using MyLibrary;
using Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Updater;
using Yandex;

namespace Agot2Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class Service : IService
    {
        #region Host
        private WebHttpService _WebHttpService;
        private ServiceHost _ServiceHost;
        private UpdaterServer _UpdaterService = new UpdaterServer("Agot2ClientUpdaterSettings", true);
        private GamePortalServer _GamePortalService = new GamePortalServer();
        private YandexService _YandexService = new YandexService();

        //PayPalService _PayPalService = new PayPalService();

        private NotifiServer _NotifyService = new NotifiServer();
        private List<GameHost> _GameHostsL = new List<GameHost>();
        //ChatService<ChatServiceParam> _ChatService = new ChatService<ChatServiceParam>(new ChatServiceParam(), 100);
        public static List<WebSocketModel> WebSockServiceList = new List<WebSocketModel>();

        public Service()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            _YandexService.NewPayment += _GamePortalService.YandexMoneyPayment;
            //_PayPalService.NewPayment += _GamePortalService.PayPalPayment;

            //отправляет игры в систему уведомления пользователя
            _NotifyService.NewClient += (login) =>
            {
                using  (GameService.Agot2p6Entities dbContext = new Agot2p6Entities())
                {
                    return dbContext.Game.ToList()
                        .Where(p => p.CloseTime == null && GameHost.GameTypes.All(p1 => p.Id != p1.GameId))
                        .Select(p => p.ToWCFGame()).ToList();
                }
            };

            NotifiServer.AddOnlineUserFunc = _GamePortalService.AddOnlineUser;
            GamePortalServer.AddUserNotifiFunc = _NotifyService.AddUserNotifi;
            GamePortalServer.UserInviteFunc = _NotifyService.InviteUser;
            GamePortalServer.ChangeGameWhenLinkAccounts = this.ChangeGameWhenLinkAccounts;
            GameHost.AddGameNotifiFunc = _NotifyService.AddGameNotifi;
            GameHost.AddUserNotifiFunc = _NotifyService.AddUserNotifi;
            GameHost.UserInviteFunc = _NotifyService.InviteUser;

            SaveStaticData();
            GameServiceInitialize();            
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exp = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject.ToString());
            GameException.NewGameException(null, "Неизвестная ошибка.", exp, false);
            File.WriteAllText("Exception.txt", exp.ToString());

            this.Dispose();
        }

        public void Start()
        {
            _UpdaterService.Start();
            _YandexService.Start();
            // _PayPalService.Start();

            _NotifyService.Start();
            _GamePortalService.Start();

            _ServiceHost = new ServiceHost(this);
            _ServiceHost.Open();

            _WebHttpService = new WebHttpService();

            //lock (Service.WebSockServiceList)
            {
                Service.WebSockServiceList.Add(new ChatService(Guid.Empty, 100));
            }
        }

        public void Dispose()
        {
            _WebHttpService.Dispose();
            _ServiceHost.Abort();
            WebSockServiceList.ForEach(p => p.Dispose());
            _GamePortalService.Dispose();
            _NotifyService.Dispose();

            _YandexService.Dispose();
            //_PayPalService.Dispose();

            _UpdaterService.Dispose();
            _GameHostsL.ToList().ForEach(p => GameHost_Closed(p));
        }
        #endregion

        #region interface
        public void SendException(WCFGameException wcfGameException)
        {
            try
            {
                if (wcfGameException == null)
                    return;

                using (Agot2p6Entities dbContext = new Agot2p6Entities())
                {
                    dbContext.GameException.Add(wcfGameException.ToGameException());
                    dbContext.SaveChanges();
                }
            }
            catch (Exception exp)
            {
            }
        }

        public WCFGame NewGame(string clientVersion, WCFGameSettings gameSettings, string gamePassword)
        {
            try
            {
                if (IsDisableNewGame || gameSettings == null || !gameSettings.CheckInput())
                    return null;

#if !DEBUG
                //проверка версии клиента
                if (!string.IsNullOrEmpty(_UpdaterService.ClientVersion) && clientVersion != _UpdaterService.ClientVersion)
                    throw new Exception($"Неверная версия клиента: login={gameSettings.CreatorLogin}."); 
#endif

                WCFUser gpUser = _GamePortalService.GetProfileByLogin(gameSettings.CreatorLogin);

                //TODO проверка может ли пользователь поставить такие условия
                /*if (gpUser == null
                    || gpUser.MindRate < gameSettings.RateSettings.MindRate
                    || gpUser.HonorRate < gameSettings.RateSettings.HonorRate
                    || gpUser.LikeRate < gameSettings.RateSettings.LikeRate
                    || gpUser.DurationHours < gameSettings.RateSettings.DurationRate)
                    return Guid.Empty;*/

#if !DEBUG //запрет на много игр
                using (Agot2p6Entities dbContext = new Agot2p6Entities())
                {
                    if (dbContext.Game.Count(p => p.CloseTime == null && (p.CreatorLogin == gameSettings.CreatorLogin || p.GameUser.Any(p1 => !string.IsNullOrEmpty(p1.HomeType) && p1.Login == gameSettings.CreatorLogin))) > 0)
                        return null;
                }
#endif

                WCFGame wcfGame = CreateGame(gameSettings, gamePassword);
                if (wcfGame == null)
                    return null;

                //поднимаем хост и сообщаем о новой игре
                NewHost(wcfGame.Id);
                _NotifyService.AddGameNotifi(wcfGame);

                return wcfGame;

            }
            catch (Exception exp)
            {
                GameException.NewGameException(null, "Не удалось создать игру.", exp, false);
                return null;
            }
        }

        public WCFService GetGame(string clientVersion, string login)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login))
                    return null;

#if !DEBUG
                //проверка версии клиента
                if (!string.IsNullOrEmpty(_UpdaterService.ClientVersion) && clientVersion != _UpdaterService.ClientVersion)
                    return null;
#endif

                _GamePortalService.AddOnlineUser(login);

                WCFService result = new WCFService() { IsDisableNewGame = IsDisableNewGame };
                using (Agot2p6Entities dbContext = new Agot2p6Entities())
                {
#if DEBUG
                    IEnumerable<Game> games = dbContext.Game.ToList().Where(p => GameHost.GameTypes.All(p1 => p.Id != p1.GameId));
#endif
#if !DEBUG
                    IQueryable<Game> games = dbContext.Game.Where(p => p.CreatorLogin != "System");
#endif
                    result.Games = games.ToList().Select(p => p.ToWCFGame()).ToList();
                }

                return result;
            }
            catch (Exception exp)
            {
                GameException.NewGameException(null, "Не удалось подготовить список игр.", exp, false);
                return null;
            }
        }

        public void UpdateGamePoint(string clientVersion, string login, WCFGamePoint newPoint)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login) || newPoint == null)
                    throw new Exception("Некоректные данные");

#if !DEBUG
                //проверка версии клиента
                if (!string.IsNullOrEmpty(_UpdaterService.ClientVersion) && clientVersion != _UpdaterService.ClientVersion)
                    throw new Exception($"Неверная версия клиента: login={login}."); 
#endif

                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    User gpUser = gamePortal.Users.FirstOrDefault(p => p.Login == login);
                    if (gpUser == null || !gpUser.Titles.Any(p => p.Name == "dynamic_title1*titleType_Создатель"))
                        throw new Exception("Доступ запрещён id" + gpUser?.Login);
                }

                using (Agot2p6Entities dbContext = new Agot2p6Entities())
                {
                    GamePoint curPoint = dbContext.GamePoint.FirstOrDefault(p => p.Id == newPoint.Id);
                    if (curPoint == null)
                        throw new Exception("Неизвестная точка");

                    curPoint.X = newPoint.X;
                    curPoint.Y = newPoint.Y;

                    dbContext.SaveChanges();
                }
            }
            catch (Exception exp)
            {
                GameException.NewGameException(null, "Не удалось изменить точку:", exp, false);
            }
        }

        public static bool IsDisableNewGame;
        public void DisableNewGame(string clientVersion, string login, bool disable)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login))
                    throw new Exception("Доступ запрещён");

#if !DEBUG
                //проверка версии клиента
                if (!string.IsNullOrEmpty(_UpdaterService.ClientVersion) && clientVersion != _UpdaterService.ClientVersion)
                    throw new Exception($"Неверная версия клиента: login={login}."); 
#endif

                using (GamePortalEntities gamePortal = new GamePortalEntities())
                {
                    User gpUser = gamePortal.Users.FirstOrDefault(p => p.Login == login);
                    if (gpUser == null || !gpUser.Titles.Any(p => p.Name == "dynamic_title1*titleType_Создатель"))
                    {
                        throw new Exception("Доступ запрещён id" + gpUser?.Login);
                    }
                }

                IsDisableNewGame = disable;
            }
            catch (Exception exp)
            {
                GameException.NewGameException(null, "Service.DisableNewGame", exp, false);
            }
        }
        #endregion

        //Востанавливаем игры
        private void GameServiceInitialize()
        {
            using (Agot2p6Entities dbContext = new Agot2p6Entities())
            {
                IEnumerable<Game> games = dbContext.Game.ToList().Where(p => GameHost.GameTypes.All(p1 => p.Id != p1.GameId));
                games.ToList().ForEach(p => NewHost(p.Id));
            }
        }

        private void SaveStaticData()
        {
            using (Agot2p6Entities dbContext = new Agot2p6Entities())
            {
                CryptoFileJson<WCFStaticData> staticData = new CryptoFileJson<WCFStaticData>(_UpdaterService.AppPath + "/staticData", "W@NtUz81")
                {
                    Value = new WCFStaticData
                    {
                        DoType = dbContext.DoType.ToList().Select(p => p.ToWCFDoType()).ToList(),
                        HomeType = dbContext.HomeType.ToList().Select(p => p.ToWCFHomeType()).ToList(),
                        OrderType = dbContext.OrderType.ToList().Select(p => p.ToWCFOrderType()).ToList(),
                        TerrainType = dbContext.TerrainType.ToList().Select(p => p.ToWCFTerrainType()).ToList(),
                        TokenType = dbContext.TokenType.ToList().Select(p => p.ToWCFTokenType()).ToList(),
                        TrackType = dbContext.TrackType.ToList().Select(p => p.ToWCFTrackType()).ToList(),
                        UnitType = dbContext.UnitType.ToList().Select(p => p.ToWCFUnitType()).ToList(),
                        GamePoint = dbContext.GamePoint.ToList().Select(p => p.ToWCFGamePoint()).ToList(),
                        ObjectPoint = dbContext.ObjectPoint.ToList().Select(p => p.ToWCFObjectPoint()).ToList(),
                        TokenPoint = dbContext.TokenPoint.ToList().Select(p => p.ToWCFTokenPoint()).ToList(),
                        TrackPoint = dbContext.TrackPoint.ToList().Select(p => p.ToWCFTrackPoint()).ToList(),
                        Terrain = dbContext.Terrain.ToList().Select(p => p.ToWCFTerrain()).ToList(),
                        TerrainTerrain = dbContext.TerrainTerrain.ToList().Select(p => p.ToWCFTerrainTerrain()).ToList(),
                        Symbolic = dbContext.Symbolic.ToList().Select(p => p.ToWCFSymbolic()).ToList(),
                        HomeCardType = dbContext.HomeCardType.ToList().Select(p => p.ToWCFHomeCardType()).ToList(),
                        VesterosCardType = dbContext.VesterosCardType.ToList().Select(p => p.ToWCFVesterosCardType()).ToList(),
                        RandomDesk = dbContext.RandomDesk.ToList().Select(p => p.ToWCFRandomDesk()).ToList()
                    }
                };//todo указать мягкий путь
                if (!staticData.Write())
                {
                    throw staticData.Exp;
                }
            }
        }

        private WCFGame CreateGame(WCFGameSettings gameSettings, string gamePassword)
        {
            using (Agot2p6Entities dbContext = new Agot2p6Entities())
            {
                //игра образец
                GameTypeItem gametype = GameHost.GameTypes.Single(p => p.Id == gameSettings.GameType);
                Game ownerGame = dbContext.Game.Single(p => p.Id == gametype.GameId);
                ownerGame.DbContext = dbContext;

                //копируем
                Game game = ownerGame.CopyGame();
                game.Type = ownerGame.Type;
                game.CreatorLogin = gameSettings.CreatorLogin;
                game.Name = gameSettings.Name;
                game.Password = gamePassword;

                //настраиваем
                game.MindRate = gameSettings.RateSettings.MindRate;
                game.HonorRate = gameSettings.RateSettings.HonorRate;
                game.LikeRate = gameSettings.RateSettings.LikeRate;
                game.DurationRate = gameSettings.RateSettings.DurationRate;
                game.RandomIndex = gameSettings.RandomIndex;
                game.IsRandomSkull = gameSettings.IsRandomSkull;
                game.MaxTime = gameSettings.MaxTime;
                game.AddTime = gameSettings.AddTime;
                game.Lang = gameSettings.Lang;
                game.WithoutChange = gameSettings.WithoutChange;
                game.IsGarrisonUp = gameSettings.IsGarrisonUp;
                game.NoTimer = gameSettings.NoTimer;

                //добавляем
                dbContext.Game.Add(game);
                dbContext.SaveChanges();

                return game.ToWCFGame();
            }
        }

        private void NewHost(Guid gameId)
        {
            GameHost gameHost = new GameHost(_GamePortalService);
            gameHost.Start(gameId);
            gameHost.Closed += GameHost_Closed;
            lock (_GameHostsL)
            {
                _GameHostsL.Add(gameHost);
            }
            lock (Service.WebSockServiceList)
            {
                Service.WebSockServiceList.Add(gameHost.ChatService);
            }
        }

        private void GameHost_Closed(GameHost obj)
        {
            lock (Service.WebSockServiceList)
            {
                Service.WebSockServiceList.Remove(obj.ChatService);
            }
            obj.Closed -= GameHost_Closed;
            obj.Dispose();
            lock (_GameHostsL)
            {
                _GameHostsL.Remove(obj);
            }
        }

        private void ChangeGameWhenLinkAccounts(string user, string linkUser)
        {
            try
            {
                using (Agot2p6Entities dbContext = new Agot2p6Entities())
                {
                    dbContext.Game.Where(p => p.CreatorLogin == linkUser).ToList().ForEach(p => p.CreatorLogin = user);
                    dbContext.GameUser.Where(p => p.Login == linkUser).ToList().ForEach(p => p.Login = user);
                    dbContext.SaveChanges();
                }
            }
            catch (Exception exp)
            {
                GameException.NewGameException(null, $"func=ChangeGameWhenLinkAccounts, user={user}, linkUser={linkUser}.", exp, false);
            }
        }
    }
}
