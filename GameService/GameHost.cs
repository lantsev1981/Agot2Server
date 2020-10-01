using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

using GamePortal;
using Notifications;
using System.Threading;
using System.Threading.Tasks;
using MyLibrary;
using System.IO;

namespace GameService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public partial class GameHost : IGameService, IDisposable
    {
        static public GameTypes GameTypes = new GameTypes();

        #region Оповещения
        //static public Action<Stream> AddChatFunc;
        static public Action<WCFGame> AddGameNotifiFunc;
        static public Action<WCFUser, string> AddUserNotifiFunc;
        #endregion

        static public readonly Config<ConfigSettings> Config = new Config<ConfigSettings>("GameService");

        public event Action<GameHost> Closed;

        List<string> _DeniedLogin { get; set; }

        //Отображаемые стрелки
        public ArrowList ArrowsData { get; set; }

        public GamePortalServer GamePortalServer { get; private set; }
        public GameHost(GamePortalServer gamePortalServer)
        {
            GamePortalServer = gamePortalServer;
            _DeniedLogin = new List<string>();
            _Timer = new Timer((o) => CheckGameActivity());
        }

        public void Dispose()
        {
            _Timer.Dispose();
            _Timer = null;
            _ServiceHost.Abort();
        }

        public Guid GameId { get; private set; }
        ServiceHost _ServiceHost;
        public void Start(Guid gameId)
        {
            GameId = gameId;

            ArrowsData = new ArrowList(GameId);
            _ServiceHost = new ServiceHost(this, new Uri($"http://{GameHost.Config.Settings.ServerAddress}:{GameHost.Config.Settings.ServerPort}/GameHost/{GameId}"));
            _ServiceHost.Open();

#if !DEBUG
            if (_Timer != null)
                _Timer.Change(_UserLiveTime, _TimerPeriod);
#endif
        }

        public WCFGame Connect(string login, string gamePassword, string homeType)
        {
            if (string.IsNullOrEmpty(login) || login == "System" || login == "Вестерос")
                return null;
            if (CheckBlackList(login) == false)
                return null;

#if !DEBUG //запрет на много игр
            using (Agot2p6Entities dbContext = new Agot2p6Entities())
            {
                if (dbContext.Game.Count(p => p.CloseTime == null && p.GameUser.Any(p1 => !string.IsNullOrEmpty(p1.HomeType) && p1.Login == login)) > 1)
                {
                    var profile = GamePortalServer.GetProfileByLogin(login);
                    if (profile.AllPower < 400 || (int)profile.UserGames.Where(p => !p.IsIgnoreHonor && p.EndTime.HasValue).Sum(p => (p.EndTime.Value - p.StartTime).TotalHours) < 48)
                        return null;
                }
            }
#endif

            var result = ConnectTask(login, gamePassword, homeType);

#if DEBUG
            /*var xml = new PublicFileJson<WCFGame>("Connect.txt");
            xml.Value = result;
            xml.Write();*/
#endif

            return result;
        }

        public bool? CheckBlackList(string login)
        {
            if (string.IsNullOrEmpty(login) || login == "System" || login == "Вестерос")
                return false;

            using (Agot2p6Entities dbContext = new Agot2p6Entities())
            {
                var game = dbContext.Game.SingleOrDefault(p => p.Id == GameId);
                var homeUser = game.HomeUsersSL;

                //уже играет
                if (homeUser.Any(p => p.Login == login))
                    return true;

                //находится в чёрном списке у одного из игроков
                var users = homeUser.Where(p => !string.IsNullOrEmpty(p.Login)).Select(p => GamePortalServer.GetProfileByLogin(p.Login)).ToList();
                if (users.Any(p => p.SpecialUsers.Any(p1 => p1.IsBlock && p1.SpecialLogin == login)))
                    return false;

                //один из игроков в чёрном списке
                var user = GamePortalServer.GetProfileByLogin(login);
                if (users.Any(p => user.SpecialUsers.Any(p1 => p1.IsBlock && p1.SpecialLogin == p.Login)))
                    return null;
            }

            return true;
        }

        //Проверяет доступность дома соблюдая последовательность подключения к базе
        public bool CheckAccessHome(string login, string homeType)
        {
            if (string.IsNullOrEmpty(homeType)) return true;
            if (_DeniedLogin.Any(p => p == login)) return false;

            return GameHost.TaskFactory.StartNew(() => { return CheckAccessHomeFunc(login, homeType); }).Result;
        }

        public void DisConnect(WCFGameUser clientUser)
        {
            if (!clientUser.CheckIn())
                return;

            DisConnectTask(clientUser);
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
                        var user = dbContext.GameUser.Single(p => p.Id == wcfStep.GameUser);
                        user.Game1.DbContext = dbContext;
                        user.Game1.GameHost = this;

                        Step serverStep = user.Game1.HomeUsersSL.Select(p => p.LastStep).Single(p => p.Id == wcfStep.Id);
                        wcfStep.Update(serverStep);

                        if (user.Game1.HomeUsersSL.SelectMany(p => p.Step).All(p => p.IsFull))
                            NextStage(user.Game1);

                        ArrowsData.AddToDbContext(user.Game1);
                        dbContext.SaveChanges();
                    }
                }
                catch (TheEndException) { }
                catch (AntiCheatException) { }
                catch (Exception exp) { GameException.NewGameException(wcfStep.Game, string.Format("Не удалось обработать ход (Failed to process the step): {0}", wcfStep != null ? wcfStep.StepType : "пустой ход"), exp, true); }
            });
        }

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
                            stepList = game.LastHomeSteps;
                            stepList.Add(game.Vesteros.LastStep);
                        }
                        //новые ходы
                        else
                            stepList = game.AllSteps.Where(p => p.Id > clientUser.LastStepIndex).ToList();
                    }
                    else
                        //требуется конкретный ход
                        stepList = game.AllSteps.Where(p => p.Id == stepIndex).ToList();
                    var result = stepList.Select(p => p.ToWCFStep()).ToList();

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
                return null;
            }
        }

        public List<WCFGameUser> GetUserInfo(WCFGameUser clientUser)
        {
            if (clientUser == null || !clientUser.CheckIn())
                return null;

            return TaskFactory.StartNew(() =>
            {
                try
                {
                    using (Agot2p6Entities dbContext = new Agot2p6Entities())
                    {
                        var user = dbContext.GameUser.SingleOrDefault(p => p.Id == clientUser.Id);
                        if (user == null || !clientUser.Check(user))
                            return null;

                        user.LastUpdate = DateTimeOffset.UtcNow;
                        var game = user.Game1;
                        List<WCFGameUser> result = game.GameUser.Select(p => p.ToWCFGameUser(user, clientUser)).ToList();
                        if (game.OpenTime == null && result.Where(p => !string.IsNullOrEmpty(p.HomeType)).All(p => !string.IsNullOrEmpty(p.Login) && p.OnLineStatus))
                        {
                            game.HomeUsersSL.ForEach(p => GamePortalServer.StartUserGame(p.Login, p.HomeType, game.Id, game.Type + (game.RandomIndex > 0 || game.IsRandomSkull ? 1 : 0)));
                            game.OpenTime = DateTimeOffset.UtcNow;
                            game.NewThink();
                        }
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
