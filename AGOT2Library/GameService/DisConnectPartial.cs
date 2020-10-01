using ChatServer;
using System;
using System.Linq;

namespace GameService
{
    public partial class GameHost
    {
        public void DisConnectTask(WCFGameUser clientUser)
        {
            using (Agot2p6Entities dbContext = new Agot2p6Entities())
            {
                GameUser user = dbContext.GameUser.SingleOrDefault(p => p.Id == clientUser.Id);
                if (user == null || !clientUser.Check(user))
                    return;

                Game game = user.Game1;
                game.GameHost = this;
                game.DbContext = dbContext;
                if (game.CreateTime < DateTimeOffset.UtcNow && game.CloseTime == null && !string.IsNullOrEmpty(user.HomeType))
                {
//                    bool hasVaule = _DeniedLogin.TryGetValue(user.Login, out int oldLiaveCount);
//                    int newLiaveCount = game.OpenTime == null ? GameHost.MaxLiaveCount + 1 : oldLiaveCount + 1;
//#if !DEBUG
//                    if (!hasVaule) _DeniedLogin.TryAdd(user.Login, newLiaveCount);
//                    else _DeniedLogin.TryUpdate(user.Login, newLiaveCount, oldLiaveCount);
//#endif

                    GamePortal.WCFUser profile = GamePortalServer.GetProfileByLogin(user.Login);

                    if (game.OpenTime != null)
                    {
                        GamePortalServer.StopUserGame(user.Login, game.Id);
#if DEBUG
                        Capitulate(game, user);
#endif
                    }
                    else
                        GameHost.AddUserNotifiFunc(profile, $"dynamic_leftGame1*unknown home");//{user.HomeType}

                    string whois = profile == null ? user.Login : profile.Api["FIO"];
                    int liaveCount = profile.UserGames.Count(p => p.GameId == this.GameId && p.EndTime.HasValue && !p.IsIgnoreHonor);
                    if (liaveCount > GameHost.MaxLiaveCount)
                        ChatService.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_Exile*homeType_{user.HomeType}*Faceless Men" });//{whois}
                    else if(game.OpenTime != null)
                        ChatService.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_leftGame*homeType_{user.HomeType}*Faceless Men*{GameHost.MaxLiaveCount - liaveCount + 1}" });//{whois}

                    user.Login = null;
                    dbContext.SaveChanges();

                    if (game.OpenTime != null)
                        GameHost.AddGameNotifiFunc(game.ToWCFGame());
                }
            }
        }
    }
}
