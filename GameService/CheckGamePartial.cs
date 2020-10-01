using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GamePortal;
using Lantsev;
using MyLibrary;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace GameService
{
    public partial class GameHost : IGameService
    {
        //Задачи требующие последовательного выполнения (изменение БД)
        static public TaskFactory TaskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(1));

        Timer _Timer { get; set; }
        static TimeSpan _GameLiveTime = TimeSpan.FromDays(1);
        static TimeSpan _AutoCloseTime = TimeSpan.FromMinutes(30);
        static TimeSpan _UserLiveTime = TimeSpan.FromMinutes(10);
        static TimeSpan _TimerDueTime = TimeSpan.FromSeconds(30);
        static TimeSpan _TimerPeriod = TimeSpan.FromMilliseconds(-1);//off

        void CheckGameActivity()
        {
            TaskFactory.StartNew(() =>
            {
                try
                {
                    using (Agot2p6Entities dbContext = new Agot2p6Entities())
                    {
                        var game = dbContext.Game.Single(p => p.Id == this.GameId);
                        game.GameHost = this;
                        game.DbContext = dbContext;

                        if (game.CreatorLogin == "System")
                            return;

                        if (game.CloseTime == null)
                        {
                            RemoveUser(game);

                            var time = DateTimeOffset.UtcNow - GameHost._AutoCloseTime;
                            if (game.OpenTime != null && game.GameUser.Any(p => !string.IsNullOrEmpty(p.HomeType) && !p.LastStep.IsFull && p.LastUpdate < time))
                                game.TheEnd();
                        }
                        //игру можно удалять и она закрыта
                        else if (!game.IsDeleteIgnore)
                        {
                            //удаление брошенных партий или завершённых (через сутки)
                            if (game.OpenTime == null || DateTimeOffset.UtcNow - game.CloseTime > GameHost._GameLiveTime)
                            {
                                dbContext.Game.Remove(game);
                                if (Closed != null)
                                    Closed(this);
                            }
                        }

                        dbContext.SaveChanges();
                    }

                }
                catch (TheEndException) { }
                catch (Exception exp)
                {
                    GameException.NewGameException(this.GameId, "Не удалось проверить активность партии.", exp, false);
                }

                if (_Timer != null)
                    _Timer.Change(_TimerDueTime, _TimerPeriod);
            });
        }

        private void RemoveUser(Game game)
        {
            var time = DateTimeOffset.UtcNow - GameHost._UserLiveTime;
            var userList = game.GameUser.Where(p => !string.IsNullOrEmpty(p.Login) && p.Login != "Вестерос" && p.LastUpdate < time);
            //if (DateTimeOffset.UtcNow - game.CreateTime < _UserLiveTime)
            //    userList = userList.Where(p => string.IsNullOrEmpty(p.HomeType));

            //лишаем лордства неактивных
            foreach (var user in userList.ToList())
            {
                var profile = GamePortalServer.GetProfileByLogin(user.Login);

                if (game.OpenTime != null)
                    GamePortalServer.StopUserGame(user.Login, game.Id);
                else if (!string.IsNullOrEmpty(user.HomeType))
                    GameHost.AddUserNotifiFunc(profile, string.Format("dynamic_leftGame1*{0}", user.HomeType));

                if (!string.IsNullOrEmpty(user.HomeType))
                {
                    var whois = profile == null ? user.Login : profile.Api["FIO"];
                    var response = ExtHttp.Post($"http://{GameHost.Config.Settings.ServerAddress}:{GameHost.Config.Settings.ServerPort}/ChatService/AddChat",
                        JsonConvert.SerializeObject(new { roomId = game.Id, creator = "Вестерос", message = $"dynamic_Exile*homeType_{user.HomeType}*{whois}" }));
                    //GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("dynamic_Exile*homeType_{0}*{1}",
                    //        user.HomeType,
                    //        profile == null ? user.Login : profile.Api["FIO"]));
                }

                //наблюдателей удаляем
                if (string.IsNullOrEmpty(user.HomeType))
                    game.DbContext.GameUser.Remove(user);
                else
                {
                    _DeniedLogin.Add(user.Login);
                    user.Login = null;

                    //сообщаем что требуется замена
                    if (game.OpenTime != null)
                        GameHost.AddGameNotifiFunc(game.ToWCFGame());
                }
            }

            //если партию покинули все игроки
            if (game.HomeUsersSL.All(p => p.Login == null))
                game.CloseTime = DateTimeOffset.UtcNow;
        }
    }


}
