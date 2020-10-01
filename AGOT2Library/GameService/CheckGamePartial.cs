using ChatServer;
using GamePortal;
using Lantsev;
using MyLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameService;

namespace GameService
{
    public partial class GameHost
    {
        private DateTimeOffset dateTimeNow;

        //проверка соединения с интернетом
        private DateTimeOffset inetConnectionTime;
        private InternetConnection lastInetStatus;
        private bool royalPardon = false;

        //Задачи требующие последовательного выполнения (изменение БД)
        public TaskFactory TaskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(1));

        private Timer _Timer { get; set; }

        private static TimeSpan _GameLiveTime = TimeSpan.FromDays(1);
        private TimeSpan _AutoCloseTime;
        private TimeSpan _UserLiveTime;
        private static TimeSpan _TimerDueTime = TimeSpan.FromSeconds(30);
        private static TimeSpan _TimerPeriod = TimeSpan.FromMilliseconds(-1);//off
        private static TimeSpan _InetRestartTime = TimeSpan.FromMinutes(10);

        private void CheckGameActivity()
        {
            TaskFactory.StartNew(() =>
            {
                InternetConnection newInetStatus = null;
                try
                {
                    #region Ожидание при отключении/подключения к интернету
                    dateTimeNow = DateTimeOffset.UtcNow;
                    newInetStatus = new InternetConnection();

                    //если произошло переподключение
                    if ((lastInetStatus == null || !lastInetStatus.IsInternetConnected) && newInetStatus.IsInternetConnected)
                    {
                        inetConnectionTime = dateTimeNow;
                        royalPardon = true;

                        //Начало проверки после запуска через...
                        _Timer?.Change(_InetRestartTime, _TimerPeriod);
                        lastInetStatus = newInetStatus;
                        return;
                    }
                    #endregion

                    if (newInetStatus.IsInternetConnected)
                    {
                        using (Agot2p6Entities dbContext = new Agot2p6Entities())
                        {
                            Game game = dbContext.Game.Single(p => p.Id == this.GameId);
                            if (game.CreatorLogin == "System")
                                return;

                            game.GameHost = this;
                            game.DbContext = dbContext;

                            if (game.CloseTime == null)
                            {
                                RemoveUser(game);

                                if (game.OpenTime != null)
                                {
                                    GameUser user = game.GameUser.FirstOrDefault(p => !string.IsNullOrEmpty(p.HomeType) && !p.LastStep.IsFull && p.LastUpdate < dateTimeNow - _AutoCloseTime);
                                    if (user != null && !Capitulate(game, user))
                                        game.TheEnd();
                                }
                            }
                            //игру можно удалять и она закрыта
                            else if (!game.IsDeleteIgnore)
                            {
                                //удаление брошенных партий или завершённых (через сутки)
                                if (game.OpenTime == null || dateTimeNow - game.CloseTime > GameHost._GameLiveTime)
                                {
                                    dbContext.Game.Remove(game);
                                    Closed?.Invoke(this);
                                }
                            }

                            dbContext.SaveChanges();
                        }
                    }
                }
                catch (TheEndException) { }
                catch (Exception exp)
                {
                    GameException.NewGameException(this.GameId, "Не удалось проверить активность партии.", exp, false);
                }

                _Timer?.Change(_TimerDueTime, _TimerPeriod);
                lastInetStatus = newInetStatus;
            });
        }

        private void RemoveUser(Game game)
        {
            DateTimeOffset time = dateTimeNow - _UserLiveTime;
            IEnumerable<GameUser> userList = game.GameUser.Where(p => !string.IsNullOrEmpty(p.Login) && p.Login != "Вестерос" && p.LastUpdate < time);

            //лишаем лордства неактивных
            foreach (GameUser user in userList.ToList())
            {
                //лорды
                if (!string.IsNullOrEmpty(user.HomeType))
                {
                    WCFUser profile = GamePortalServer.GetProfileByLogin(user.Login);
                    //                    bool hasVaule = _DeniedLogin.TryGetValue(user.Login, out int oldLiaveCount);
                    //                    int newLiaveCount = game.OpenTime == null ? GameHost.MaxLiaveCount + 1 : oldLiaveCount + 1;
                    //#if !DEBUG
                    //                    if (!hasVaule) _DeniedLogin.TryAdd(user.Login, newLiaveCount);
                    //                    else _DeniedLogin.TryUpdate(user.Login, newLiaveCount, oldLiaveCount);
                    //#endif
                    if (game.OpenTime != null)
                        GamePortalServer.StopUserGame(user.Login, game.Id, 0, royalPardon);
                    else
                        GameHost.AddUserNotifiFunc(profile, $"dynamic_leftGame1*unknown home");//{user.HomeType}

                    string whois = profile == null ? user.Login : profile.Api["FIO"];
                    int liaveCount = profile.UserGames.Count(p => p.GameId == this.GameId && p.EndTime.HasValue && !p.IsIgnoreHonor);
                    if (liaveCount > GameHost.MaxLiaveCount)
                        ChatService.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_Exile*homeType_{user.HomeType}*Faceless Men" });//{whois}
                    else if(game.OpenTime != null)
                        ChatService.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_leftGame*homeType_{user.HomeType}*Faceless Men*{GameHost.MaxLiaveCount - liaveCount + 1}" });//{whois}

                    user.Login = null;

                    //сообщаем что требуется замена
                    if (game.OpenTime != null)
                        GameHost.AddGameNotifiFunc(game.ToWCFGame());
                }
                //наблюдателей удаляем
                else
                    game.DbContext.GameUser.Remove(user);
            }

            royalPardon = false;

            //если партию покинули все игроки
            if (game.HomeUsersSL.All(p => p.Login == null))
                game.CloseTime = dateTimeNow;
        }

        private static bool Capitulate(Game game, GameUser user)
        {
            if (!game.WithoutChange)
                return false;

            //помечаем ливера
            if (game.HomeUsersSL.IndexOf(user) < game.ThroneProgress) game.ThroneProgress--;
            user.IsCapitulated = true;
            //проверяем конец игры
            if (game.HomeUsersSL.Count <= 1) game.TheEnd();
            //кэшируем последний ход ливера
            WCFStep userStep = user.LastStep.ToWCFStep();

            //определяем кто ходил (цепочка событий)
            if (--game.ThroneProgress < 0) game.ThroneProgress = game.HomeUsersSL.Count - 1;
            GameUser throneUser = game.HomeUsersSL[game.ThroneProgress];

            //определяем начало последнего действия и удаляем все ходы после него (откат)
            WCFStep backStep = throneUser.Step.Last(p => p.StepType == "Замысел" || p.StepType == "Набег" || p.StepType == "Поход" || p.StepType == "Усиление_власти").ToWCFStep();
            WCFStep backVesterosStep = game.Vesteros.LastStep.ToWCFStep();
            if (backStep.StepType != "Замысел")
            {
                IEnumerable<Step> removeSteps = game.AllSteps.Where(p => p.Id >= backStep.Id);
                game.DbContext.Step.RemoveRange(removeSteps);
                NewDoStep(game, game.DbContext.DoType.Single(p => p.Name == backStep.StepType), game.ThroneProgress);
            }
            else
            {
                backStep = game.Vesteros.Step.Last(p => p.StepType == "Замысел").ToWCFStep();
                IEnumerable<Step> removeSteps = game.AllSteps.Where(p => p.Id >= backStep.Id);
                game.DbContext.Step.RemoveRange(removeSteps);
                game.NewThink();
            }

            //дублируем информируем о ливнувших домах
            game.GameUser.Where(p => p.IsCapitulated).ToList().ForEach(p =>
            {
                Step step = p.LastStep.CopyStep("Победа", true);
                step.NewMessage($"dynamic_leftGame*{p.HomeType}**0");
            });

            //Состояние Вестероса
            Step newVesterosStep = game.Vesteros.LastStep.CopyStep(backStep.StepType != "Замысел" ? "Default" : "Замысел", true);
            game.ResetVesterosDesc(1);
            game.ResetVesterosDesc(2);
            game.ResetVesterosDesc(3);
            game.ResetVesterosDesc(4);

            //Проверяем наличие гарнизонов предыдущих ливеров
            foreach (WCFGarrison item in backVesterosStep.GameInfo.Garrison)
            {
                if (!newVesterosStep.GameInfo.Garrison.Any(p => p.Terrain == item.Terrain))
                {
                    Garrison result = new Garrison
                    {
                        Id = Guid.NewGuid(),
                        Step = newVesterosStep.Id,
                        Game = newVesterosStep.Game,
                        TokenType = "Гарнизон",
                        Terrain = item.Terrain,
                        Strength = item.Strength
                    };
                    newVesterosStep.GameInfo.Garrison.Add(result);
                }
            }
            //Заменяем войска ливера на гарнизоны
            foreach (WCFGameUserTerrain item in userStep.GameUserInfo.GameUserTerrain)
            {
                int strength = userStep.GameUserInfo.Unit.Where(p => p.Terrain == item.Terrain).Sum(p => game.DbContext.UnitType.First(p1 => p1.Name == p.UnitType).Cost);
                if (userStep.GameUserInfo.PowerCounter.SingleOrDefault(p => p.Terrain == item.Terrain) != null)
                    strength++;
                if (strength != 0)
                {
                    Garrison garrison = newVesterosStep.GameInfo.Garrison.SingleOrDefault(p => p.Terrain == item.Terrain);
                    if (garrison != null)
                        garrison.Strength += strength;
                    else
                    {
                        Garrison result = new Garrison
                        {
                            Id = Guid.NewGuid(),
                            Step = newVesterosStep.Id,
                            Game = newVesterosStep.Game,
                            TokenType = "Гарнизон",
                            Terrain = item.Terrain,
                            Strength = strength
                        };
                        newVesterosStep.GameInfo.Garrison.Add(result);
                    }
                }
            }

            int i = 1;
            foreach (Step item in game.LastHomeSteps.OrderBy(p => p.GameUserInfo.ThroneInfluence))
            {
                if (item.GameUserInfo.ThroneInfluence != i)
                {
                    Step step = item.CopyStep("Борьба_за_влияние", true);
                    step.NewMessage($"dynamic_voting*voting_Железный_трон*{i}");
                    step.GameUserInfo.ThroneInfluence = i;
                }
                i++;
            }
            i = 1;
            foreach (Step item in game.LastHomeSteps.OrderBy(p => p.GameUserInfo.BladeInfluence))
            {
                if (item.GameUserInfo.BladeInfluence != i)
                {
                    Step step = item.CopyStep("Борьба_за_влияние", true);
                    step.NewMessage($"dynamic_voting*voting_Вотчины*{i}");
                    step.GameUserInfo.BladeInfluence = i;
                }
                i++;
            }
            i = 1;
            foreach (Step item in game.LastHomeSteps.OrderBy(p => p.GameUserInfo.RavenInfluence))
            {
                if (item.GameUserInfo.RavenInfluence != i)
                {
                    Step step = item.CopyStep("Борьба_за_влияние", true);
                    step.NewMessage($"dynamic_voting*voting_Королевский_двор*{i}");
                    step.GameUserInfo.RavenInfluence = i;
                }
                i++;
            }

            return true;
        }
    }
}
