using ChatServer;
using GamePortal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameService
{
    public partial class GameHost
    {
        private WCFGame ConnectTask(string login, string gamePassword, string homeType)
        {
            //если дом занят или недоступен то наблюдатель
            if (homeType != null && !CheckAccessHomeFunc(login, homeType))
                homeType = null;

            using (Agot2p6Entities dbContext = new Agot2p6Entities())
            {
                WCFUser profile = GamePortalServer.GetProfileByLogin(login);
                Game game = dbContext.Game.Single(p => p.Id == GameId);
                GameUser gameUser = game.GameUser.SingleOrDefault(p => p.Login == login);

                //Пользователь уже принимал участие
                //_DeniedLogin.TryGetValue(login, out int liaveCount);
                IEnumerable<WCFUserGame> usergames = profile.UserGames.Where(p => p.GameId == this.GameId);
                if (usergames.Count(p => p.EndTime.HasValue && !p.IsIgnoreHonor) > GameHost.MaxLiaveCount)
                {
                    homeType = null;
                    ChatService.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_ExileEarlier*{profile.Api["FIO"]}" });
                }
                else
                {
                    //потенциаьный лорд
                    if (gameUser == null || gameUser.HomeType == null)
                    {
                        //выбран дом и игра не закрыта
                        if (!string.IsNullOrEmpty(homeType) && game.CloseTime == null)
                        {
                            //пароль отсутствует или указан верно
                            if (string.IsNullOrEmpty(game.Password) || game.Password == gamePassword)
                            {
                                //Удаляем наблюдателя
                                if (gameUser != null && gameUser.HomeType == null)
                                {
                                    dbContext.GameUser.Remove(gameUser);
                                    gameUser = null;
                                }

                                //конкретный дом
                                if (homeType != "Random")
                                {
                                    gameUser = game.HomeUsersSL.SingleOrDefault(p => p.HomeType == homeType);
                                    gameUser.Login = login;
                                }
                                //Случайный дом
                                else
                                {
                                    //Определяем свободные дома и сортируем по имени дома
                                    List<GameUser> freeHome = game.HomeUsersSL.Where(p => p.Login == null).OrderBy(p => p.HomeType).ToList();

                                    //если игрок ранее заходил в эту игру, то определяем за какой дом в последний раз он играл
                                    WCFUserGame lastgame = usergames.OrderBy(p => p.StartTime).LastOrDefault();
                                    if (lastgame != null)
                                    {
                                        //Если этот дом свободен, то отдаём его пользователю
                                        gameUser = freeHome.FirstOrDefault(p => p.HomeType == lastgame.HomeType);
                                        if (gameUser != null) gameUser.Login = login;
                                    }

                                    //если дом не присвоен
                                    if (gameUser == null)
                                    {
                                        //отдаём тот дом за который пользователь играл меньше всего
                                        gameUser = freeHome.OrderBy(p => profile.UserGames.Count(p1 => p1.HomeType == p.HomeType)).First();
                                        gameUser.Login = login;
                                    }

                                    //if (lastgame == null)
                                    //{
                                    //    int index = GameHost.Rnd.Next(freeHome.Count());
                                    //    gameUser = freeHome.ElementAt(index);
                                    //    gameUser.Login = login;
                                    //}
                                }

                                ChatService.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_hiLord*Faceless Men*homeType_{gameUser.HomeType}" });//profile.Api["FIO"]
                                GameHost.AddUserNotifiFunc(profile, $"dynamic_inGame*{game.Name ?? "text_newGame"}*unknown home");//{gameUser.HomeType}
                                if (game.OpenTime != null)
                                    GamePortalServer.StartUserGame(gameUser.Login, gameUser.HomeType, game.Id, game.Type + (game.RandomIndex > 0 || game.IsRandomSkull ? 1 : 0), game.NoTimer, true);
                            }
                            else
                                ChatService.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_passwordDenied*Faceless Men" });//{profile.Api["FIO"]}
                        }
                    }
                }

                //наблюдатель
                if (gameUser == null)
                {
                    gameUser = new GameUser(game)
                    {
                        Login = login
                    };

                    game.GameUser.Add(gameUser);
                }

                gameUser.LastUpdate = DateTimeOffset.UtcNow;
                gameUser.NeedReConnect = false;


                if (game.OpenTime == null && game.HomeUsersSL.All(p => !string.IsNullOrEmpty(p.Login) && (DateTimeOffset.UtcNow - p.LastUpdate) < new TimeSpan(0, 0, 5)))
                {
                    game.HomeUsersSL.ForEach(p => GamePortalServer.StartUserGame(p.Login, p.HomeType, game.Id, game.Type + (game.RandomIndex > 0 || game.IsRandomSkull ? 1 : 0), game.NoTimer));
                    game.OpenTime = DateTimeOffset.UtcNow;
                    game.NewThink();
                }

                dbContext.SaveChanges();

                System.Collections.Generic.IEnumerable<GameUser> gameUsers = game.HomeUsersSL.Where(p => p.Login != null);
                var privateDate = gameUsers.Select(p => new { gameUser = p, privateDate = GamePortalServer.GetPrivateProfileData(p.Login) }).ToList();
                var groupe = privateDate.GroupBy(p => p.privateDate["clientId"]).Where(p => p.Count() > 1).ToList();
                groupe.AddRange(privateDate.GroupBy(p => p.privateDate["email"]).Where(p => p.Key != null && p.Count() > 1).ToList());
                groupe.AddRange(privateDate.GroupBy(p => p.privateDate["password"]).Where(p => p.Count() > 1).ToList());
                groupe.SelectMany(p => p.Select(p1 => string.Concat(p.Select(p2 => "\n" + p2.gameUser.HomeType)))).Distinct().ToList().ForEach(p =>
                {
                    ChatService.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_samePC*{p}" });
                });

                return game.ToWCFGame();
            }
        }

        private bool CheckAccessHomeFunc(string login, string homeType)
        {
            //наблюдатель
            if (string.IsNullOrEmpty(homeType))
                return true;
            //выбранный дом
            if (homeType != "Random" && GamePortalServer.GetProfileByLogin(login).AllPower < 400)
                return false;

            using (Agot2p6Entities dbContext = new Agot2p6Entities())
            {
                Game game = dbContext.Game.Single(p => p.Id == GameId);
                List<GameUser> freeHome = game.HomeUsersSL.Where(p => p.Login == null).ToList();
                //доступных домов нет
                if (freeHome.Count == 0)
                    return false;
                //выбранный дом занят или не существует
                if (homeType != "Random" && !freeHome.Any(p => p.HomeType == homeType))
                    return false;

                return true;
            }
        }
    }
}
