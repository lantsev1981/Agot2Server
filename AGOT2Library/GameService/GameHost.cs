using ChatServer;
using GamePortal;
using MyLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace GameService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public partial class GameHost : IGameService, IDisposable
    {
        public static GameTypes GameTypes = new GameTypes();
        public static readonly Config<ConfigSettings> Config = new Config<ConfigSettings>("GameService");

        #region Оповещения
        //static public Action<Stream> AddChatFunc;
        public static Action<WCFGame> AddGameNotifiFunc;
        public static Action<WCFUser, string> AddUserNotifiFunc;
        public static Func<WCFUser, string, string, bool> UserInviteFunc;
        public event Action<GameHost> Closed;
        #endregion

        public Guid GameId { get; private set; }
        public ArrowList ArrowsData { get; set; }//Отображаемые стрелки
        public GamePortalServer GamePortalServer { get; private set; }
        public ChatService ChatService { get; private set; }

        private ServiceHost _ServiceHost;

        //private ConcurrentDictionary<string, int> _DeniedLogin { get; set; }
        private static readonly int MaxLiaveCount = 1;

        public GameHost(GamePortalServer gamePortalServer)
        {
            GamePortalServer = gamePortalServer;
            //_DeniedLogin = new ConcurrentDictionary<string, int>();
            _Timer = new Timer((o) => CheckGameActivity());
        }

        public void Dispose()
        {
            _Timer.Dispose();
            _Timer = null;
            ChatService.Dispose();
            _ServiceHost.Abort();
        }

        public void Start(Guid gameId)
        {
            GameId = gameId;
            using (Agot2p6Entities dbContext = new Agot2p6Entities())
            {
                Game game = dbContext.Game.SingleOrDefault(p => p.Id == GameId);
                //_AutoCloseTime = TimeSpan.FromSeconds(game.MaxTime) + _InetRestartTime;
                _AutoCloseTime = TimeSpan.FromMinutes(5);
                _UserLiveTime = TimeSpan.FromSeconds(game.MaxTime);
            }

            ArrowsData = new ArrowList(GameId);
            _ServiceHost = new ServiceHost(this, new Uri($"http://{GameHost.Config.Settings.ServerAddress}:{GameHost.Config.Settings.ServerPort}/GameHost/{GameId}"));
            _ServiceHost.Open();

            ChatService = new ChatService(gameId, 100);

#if !DEBUG
            _Timer?.Change(_InetRestartTime, _TimerPeriod);
#endif
        }

        public WCFGame Connect(string login, string gamePassword, string homeType)
        {
            return TaskFactory.StartNew<WCFGame>(() =>
            {
                try
                {
                    if (CheckBlackList(login) == false)
                        return null;

#if !DEBUG //запрет на много игр
                    using (Agot2p6Entities dbContext = new Agot2p6Entities())
                    {
                        if (!dbContext.GameUser.Any(p1 => !string.IsNullOrEmpty(p1.HomeType) && p1.Login == login)
                            && dbContext.Game.Count(p => p.CloseTime == null
                                && p.GameUser.Any(p1 => !string.IsNullOrEmpty(p1.HomeType) && p1.Login == login)) > 1)
                        {
                            WCFUser profile = GamePortalServer.GetProfileByLogin(login);
                            if (profile.AllPower < 400 || (int)profile.UserGames.Where(p => !p.IsIgnoreHonor && p.EndTime.HasValue).Sum(p => (p.EndTime.Value - p.StartTime).TotalHours) < 48)
                                return null;
                        }
                    }
#endif

                    return ConnectTask(login, gamePassword, homeType);
                }
                catch (Exception exp)
                {
                    GameException.NewGameException(GameId, "Не удалось подключиться к игре.", exp, false);
                    return null;
                }
            }).Result;
        }

        public bool? CheckBlackList(string login)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login) || login == "System" || login == "Вестерос")
                    return false;

                using (Agot2p6Entities dbContext = new Agot2p6Entities())
                {
                    Game game = dbContext.Game.SingleOrDefault(p => p.Id == GameId);
                    List<GameUser> homeUser = game.HomeUsersSL;

                    //уже играл
                    WCFUser user = GamePortalServer.GetProfileByLogin(login);
                    if (user.UserGames.Any(p => p.GameId == this.GameId))
                        return true;

                    //находится в чёрном списке у одного из игроков
                    List<WCFUser> users = homeUser.Where(p => !string.IsNullOrEmpty(p.Login)).Select(p => GamePortalServer.GetProfileByLogin(p.Login)).ToList();
                    if (users.Any(p => p.SpecialUsers.Any(p1 => p1.IsBlock && p1.SpecialLogin == login)))
                        return false;

                    //один из игроков в чёрном списке
                    user = GamePortalServer.GetProfileByLogin(login);
                    if (users.Any(p => user.SpecialUsers.Any(p1 => p1.IsBlock && p1.SpecialLogin == p.Login)))
                        return null;
                }

                return true;
            }
            catch (Exception exp)
            {
                GameException.NewGameException(GameId, "CheckBlackList", exp, false);
                return false;
            }
        }

        //Проверяет доступность дома соблюдая последовательность подключения к базе
        public bool CheckAccessHome(string login, string homeType)
        {
            try
            {
                if (string.IsNullOrEmpty(homeType))
                    return true;
                else
                {
                    WCFUser profile = GamePortalServer.GetProfileByLogin(login);
                    int liaveCount = profile.UserGames.Count(p => p.GameId == this.GameId && p.EndTime.HasValue && !p.IsIgnoreHonor);
                    if (liaveCount > GameHost.MaxLiaveCount)
                        return false;
                }

                return CheckAccessHomeFunc(login, homeType);
            }
            catch (Exception exp)
            {
                GameException.NewGameException(GameId, "Не удалось проверить доступность дома.", exp, false);
                return false;
            }
        }

        public void DisConnect(WCFGameUser clientUser)
        {
            TaskFactory.StartNew(() =>
            {
                try
                {
                    if (!clientUser.CheckIn())
                        return;

                    DisConnectTask(clientUser);
                }
                catch (Exception exp)
                {
                    GameException.NewGameException(clientUser.Game, "Не удалось отключиться от игры.", exp, false);
                }
            });
        }


        public void SendStep(WCFStep wcfStep)
        {
            TaskFactory.StartNew(() =>
            {

#if DEBUG
                /*var xml = new PublicFileJson<WCFStep>("SendStep.txt");
                xml.Value = wcfStep;
                xml.Write();*/
#endif
                try
                {
                    using (Agot2p6Entities dbContext = new Agot2p6Entities())
                    {
                        GameUser user = dbContext.GameUser.Single(p => p.Id == wcfStep.GameUser);
                        user.Game1.DbContext = dbContext;
                        user.Game1.GameHost = this;

                        Step serverStep = user.Game1.HomeUsersSL.Select(p => p.LastStep).Single(p => p.Id == wcfStep.Id);
                        wcfStep.Update(serverStep);

                        if (user.Game1.LastHomeSteps.All(p => p.IsFull))
                            NextStage(user.Game1);

                        ArrowsData.AddToDbContext(user.Game1);
                        dbContext.SaveChanges();
                    }
                }
                catch (TheEndException) { }
                catch (AntiCheatException) { }
                catch (Exception exp)
                {
                    GameException.NewGameException(wcfStep.Game, string.Format("Не удалось обработать ход (Failed to process the step): {0}", wcfStep != null ? wcfStep.StepType : "пустой ход"), exp, true);
                }
            });
        }

        // GameService.GameHost.GetStep	90 050	1	17,28%	0,00%
        public List<WCFStep> GetStep(WCFGameUser clientUser, int stepIndex = 0)
        {
            try
            {
                if (stepIndex < 0 || clientUser == null)
                    return null;

                using (Agot2p6Entities dbContext = new Agot2p6Entities())
                {
                    GameUser user = dbContext.GameUser.SingleOrDefault(p => p.Id == clientUser.Id);
                    if (user == null)
                        return null;

                    Game game = user.Game1;
                    game.GameHost = this;
                    game.CurrentUser = user;
                    List<Step> stepList = null;
                    if (stepIndex == 0)
                    {
                        //первый забор
                        if (clientUser.LastStepIndex == 0)
                        {
                            //stepList = game.GameUser.Where(p => !string.IsNullOrEmpty(p.HomeType)).Select(p => p.LastStep).ToList();
                            stepList = game.LastHomeSteps.ToList();
                            stepList.Add(game.Vesteros.LastStep);
                        }
                        //новые ходы
                        else
                            stepList = game.AllSteps.Where(p => p.Id > clientUser.LastStepIndex).ToList();
                    }
                    else
                    {
                        //требуется конкретный ход
                        stepList = new List<Step> { game.AllSteps.First(p => p.Id >= stepIndex) };
                    }
                    List<WCFStep> result = stepList.Select(p => p.ToWCFStep()).ToList();

#if DEBUG
                    /*var xml = new PublicFileJson<List<WCFStep>>("GetStep.txt");
                    xml.Value = result;
                    xml.Write();*/
#endif

                    return result;
                }
            }
            catch (Exception exp)
            {
                GameException.NewGameException(clientUser.Game, "Не удалось подготовить список ходов.", exp, false);
                throw;
            }
        }

        // GameService.GameHost+<>c__DisplayClass60_0.<GetUserInfo>b__0	47 245	13	9,07%	0,00%
        public List<WCFGameUser> GetUserInfo(WCFGameUser clientUser)
        {
            return TaskFactory.StartNew(() =>
            {
                try
                {
                    if (clientUser == null || !clientUser.CheckIn())
                        return null;

                    using (Agot2p6Entities dbContext = new Agot2p6Entities())
                    {
                        GameUser user = dbContext.GameUser.SingleOrDefault(p => p.Id == clientUser.Id);
                        if (user == null || !clientUser.Check(user))
                            return null;

                        user.LastUpdate = DateTimeOffset.UtcNow;
                        Game game = user.Game1;
                        List<WCFGameUser> result = game.GameUser.Select(p => p.ToWCFGameUser(user, clientUser)).ToList();

                        dbContext.SaveChanges();

#if DEBUG
                        /*var xml = new PublicFileJson<List<WCFGameUser>>("GetUserInfo.txt");
                        xml.Value = result;
                        xml.Write();*/
#endif

                        return result;
                    }
                }
                catch (Exception exp)
                {
                    GameException.NewGameException(clientUser.Game, "Не удалось подготовить спискок игроков.", exp, false);
                    return null;
                }
            }).Result;
        }
    }
}
