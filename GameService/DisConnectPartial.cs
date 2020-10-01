using MyLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameService
{
    public partial class GameHost : IGameService
    {
        public void DisConnectTask(WCFGameUser clientUser)
        {
            GameHost.TaskFactory.StartNew(() =>
            {
                try
                {
                    using (Agot2p6Entities dbContext = new Agot2p6Entities())
                    {
                        GameUser user = dbContext.GameUser.SingleOrDefault(p => p.Id == clientUser.Id);
                        if (user == null || !clientUser.Check(user))
                            return;

                        Game game = user.Game1;
                        if (game.CreateTime < DateTimeOffset.UtcNow && game.CloseTime == null && !string.IsNullOrEmpty(user.HomeType))
                        {
                            var profile = GamePortalServer.GetProfileByLogin(user.Login);

                            if (game.OpenTime != null)
                                GamePortalServer.StopUserGame(user.Login, game.Id);
                            else GameHost.AddUserNotifiFunc(profile, string.Format("dynamic_leftGame1*{0}", user.HomeType));

                            if (!string.IsNullOrEmpty(user.HomeType))
                            {
                                var whois = profile == null ? user.Login : profile.Api["FIO"];
                                var response = ExtHttp.Post($"http://{GameHost.Config.Settings.ServerAddress}:{GameHost.Config.Settings.ServerPort}/ChatService/AddChat",
                                    JsonConvert.SerializeObject(new { roomId = game.Id, creator = "Вестерос", message = $"dynamic_Exile*{user.HomeType}*{whois}" }));
                                //GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("dynamic_Exile*homeType_{0}*{1}",
                                //    user.HomeType,
                                //    profile == null ? user.Login : profile.Api["FIO"]));
                            }

                            _DeniedLogin.Add(user.Login);
                            user.Login = null;
                            dbContext.SaveChanges();

                            //TODO перенести в Realease
                            if (game.OpenTime != null)
                                GameHost.AddGameNotifiFunc(game.ToWCFGame());
                        }
                    }
                }
                catch (Exception exp)
                {
                    GameException.NewGameException(clientUser.Game, "Не удалось отключиться от игры.", exp, false);
                }
            });
        }
    }
}
