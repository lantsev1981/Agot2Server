using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GamePortal;
using Newtonsoft.Json;
using MyLibrary;

namespace GameService
{
    public partial class GameHost : IGameService
    {
        WCFGame ConnectTask(string login, string gamePassword, string homeType)
        {
            return GameHost.TaskFactory.StartNew(() =>
            {
                try
                {
                    //если дом занят или недоступен то наблюдатель
                    if (homeType != null && !CheckAccessHomeFunc(login, homeType))
                        homeType = null;

                    using (Agot2p6Entities dbContext = new Agot2p6Entities())
                    {
                        var profile = GamePortalServer.GetProfileByLogin(login);
                        var game = dbContext.Game.Single(p => p.Id == GameId);
                        var gameUser = game.GameUser.SingleOrDefault(p => p.Login == login);

                        //Пользователь уже принимал участие
                        if (_DeniedLogin.Any(p => p == login))
                        {
                            homeType = null;
                            var whois = profile.Api["FIO"];
                            var response = ExtHttp.Post($"http://{GameHost.Config.Settings.ServerAddress}:{GameHost.Config.Settings.ServerPort}/ChatService/AddChat",
                                JsonConvert.SerializeObject(new { roomId = game.Id, creator = "Вестерос", message = $"dynamic_ExileEarlier*{whois}" }));
                            //GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("dynamic_ExileEarlier*{0}", profile.Api["FIO"]));
                        }
                        else
                        {
                            //потенциаьный лорд
                            if (gameUser == null || gameUser.HomeType == null)
                            {
                                //выбран дом и игра не закрыта
                                if (!string.IsNullOrEmpty(homeType) && game.CloseTime == null)
                                {
                                    //игра открыта или проходит по рейтингу
                                    //if (game.OpenTime != null || profile.CheckRateAllow(game.RateSettings))
                                    {
                                        //небыл изгнан ранее
                                        if (!profile.UserGames.Any(p => p.GameId == GameId && p.EndTime.HasValue))
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
                                                    var freeHome = game.HomeUsersSL.Where(p => p.Login == null).ToList();
                                                    int index = GameHost.Rnd.Next(freeHome.Count());
                                                    gameUser = freeHome.ElementAt(index);
                                                    gameUser.Login = login;
                                                }

                                                var whois = profile.Api["FIO"];
                                                var response = ExtHttp.Post($"http://{GameHost.Config.Settings.ServerAddress}:{GameHost.Config.Settings.ServerPort}/ChatService/AddChat",
                                                    JsonConvert.SerializeObject(new { roomId = game.Id, creator = "Вестерос", message = $"dynamic_hiLord*{whois}*homeType_{gameUser.HomeType}" }));
                                                //GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("dynamic_hiLord*{0}*homeType_{1}", profile.Api["FIO"], gameUser.HomeType));
                                                GameHost.AddUserNotifiFunc(profile, string.Format("dynamic_inGame*{0}*{1}", string.IsNullOrEmpty(game.Name) ? "text_newGame" : game.Name, gameUser.HomeType));
                                                if (game.OpenTime != null)
                                                    GamePortalServer.StartUserGame(gameUser.Login, gameUser.HomeType, game.Id, game.Type + (game.RandomIndex > 0 || game.IsRandomSkull ? 1 : 0), true);
                                            }
                                            else
                                            {
                                                var whois = profile.Api["FIO"];
                                                var response = ExtHttp.Post($"http://{GameHost.Config.Settings.ServerAddress}:{GameHost.Config.Settings.ServerPort}/ChatService/AddChat",
                                                    JsonConvert.SerializeObject(new { roomId = game.Id, creator = "Вестерос", message = $"dynamic_passwordDenied*{whois}" }));
                                                //GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("dynamic_passwordDenied*{0}", profile.Api["FIO"]));
                                            }
                                        }
                                        else
                                        {
                                            _DeniedLogin.Add(login);
                                            var whois = profile.Api["FIO"];
                                            var response = ExtHttp.Post($"http://{GameHost.Config.Settings.ServerAddress}:{GameHost.Config.Settings.ServerPort}/ChatService/AddChat",
                                                JsonConvert.SerializeObject(new { roomId = game.Id, creator = "Вестерос", message = $"dynamic_ExileEarlier*{whois}" }));
                                            //GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("dynamic_ExileEarlier*{0}", profile.Api["FIO"]));
                                        }
                                    }
                                    //else  GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("{0}, у вас низкий рейтинг.", profile.Api["FIO"]));
                                }
                            }
                        }

                        //наблюдатель
                        if (gameUser == null)
                        {
                            gameUser = new GameUser(game);
                            gameUser.Login = login;

                            //if (game.CloseTime == null)
                            //     GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("{0}, приветствуем Вас!", profile.Api["FIO"]));

                            game.GameUser.Add(gameUser);
                        }

                        gameUser.LastUpdate = DateTimeOffset.UtcNow;
                        gameUser.NeedReConnect = false;
                        dbContext.SaveChanges();

                        /*foreach (var item in game.HomeUsersSL.Where(p => p.Login != null).GroupBy(p => GamePortalServer.GetProfileByLogin(p.Login).ClientId).Where(p => p.Count() > 1).ToList())
                            GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("dynamic_samePC*{0}", String.Concat(item.Select(p => "\n" + p.HomeType))));*/

                        var gameUsers = game.HomeUsersSL.Where(p => p.Login != null);
                        var privateDate = gameUsers.Select(p => new { gameUser = p, privateDate = GamePortalServer.GetPrivateProfileData(p.Login) }).ToList();
                        var groupe = privateDate.GroupBy(p => p.privateDate["clientId"]).Where(p => p.Count() > 1).ToList();
                        groupe.AddRange(privateDate.GroupBy(p => p.privateDate["email"]).Where(p => p.Key != null && p.Count() > 1).ToList());
                        groupe.AddRange(privateDate.GroupBy(p => p.privateDate["password"]).Where(p => p.Count() > 1).ToList());
                        groupe.SelectMany(p => p.Select(p1 => String.Concat(p.Select(p2 => "\n" + p2.gameUser.HomeType)))).Distinct().ToList().ForEach(p =>
                        {
                            var whois = profile.Api["FIO"];
                            var response = ExtHttp.Post($"http://{GameHost.Config.Settings.ServerAddress}:{GameHost.Config.Settings.ServerPort}/ChatService/AddChat",
                                JsonConvert.SerializeObject(new { roomId = game.Id, creator = "Вестерос", message = $"dynamic_samePC*{p}" }));
                            //GameHost.AddChatFunc(game.Id, "Вестерос", string.Format("dynamic_samePC*{0}", p));
                        });

                        return game.ToWCFGame();
                    }
                }
                catch (Exception exp)
                {
                    GameException.NewGameException(GameId, "Не удалось подключиться к игре.", exp, false);
                    return null;
                }
            }).Result;
        }

        private bool CheckAccessHomeFunc(string login, string homeType)
        {
            //наблюдатель
            if (string.IsNullOrEmpty(homeType)) return true;
            //выбранный дом
            if (homeType != "Random" && GamePortalServer.GetProfileByLogin(login).AllPower < 400) return false;

            try
            {
                using (Agot2p6Entities dbContext = new Agot2p6Entities())
                {
                    var game = dbContext.Game.Single(p => p.Id == GameId);
                    var freeHome = game.HomeUsersSL.Where(p => p.Login == null).ToList();
                    //доступных домов нет
                    if (freeHome.Count == 0) return false;
                    //выбранный дом занят или не существует
                    if (homeType != "Random" && !freeHome.Any(p => p.HomeType == homeType)) return false;

                    return true;
                }
            }
            catch (Exception exp) { GameException.NewGameException(GameId, "Не удалось проверить доступность дома.", exp, false); return false; }
        }
    }
}
