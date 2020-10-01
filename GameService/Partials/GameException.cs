using MyLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//

namespace GameService
{
    public partial class GameException
    {
        public GameException()
        {
            this.Id = Guid.NewGuid();
            this.CreateTime = DateTime.Now;
        }

        public static void NewGameException(Guid gameId, string specialText, Exception exp, bool sendMsg)
        {
            GameException result = new GameException();
            result.Game = gameId;
            result.Login = "Вестерос";
            result.Message = string.Format("{0}\n{1}", specialText, exp.ToString());

            GameHost.TaskFactory.StartNew(() =>
            {
                using (Agot2p6Entities agot2p6Entities = new Agot2p6Entities())
                {
                    agot2p6Entities.GameException.Add(result);
                    agot2p6Entities.SaveChanges();
                }
            });

            if (sendMsg)
            {
                var response = ExtHttp.Post($"http://{GameHost.Config.Settings.ServerAddress}/ChatService/AddChat",
                    JsonConvert.SerializeObject(new { roomId = gameId, creator = "Вестерос", message = $"dynamic_error*error_criticalError*!\n{specialText}\n{exp.Message}" }));
                //GameHost.AddChatFunc(gameId, "Вестерос", string.Format("dynamic_error*error_criticalError*!\n{0}\n{1}", specialText, exp.Message));
            }
        }
    }

    public class AntiCheatException : Exception
    {
        public AntiCheatException(Guid userId, string message)
        {
            GameHost.TaskFactory.StartNew(() =>
            {
                Guid gameId = Guid.Empty;
                try
                {
                    using (Agot2p6Entities agot2p6Entities = new Agot2p6Entities())
                    {
                        var user = agot2p6Entities.GameUser.Single(p => p.Id == userId);
                        gameId = user.Game;
                        user.NeedReConnect = true;
                        agot2p6Entities.SaveChanges();
                        
                        var response = ExtHttp.Post($"http://{GameHost.Config.Settings.ServerAddress}/ChatService/AddChat",
                            JsonConvert.SerializeObject(new { roomId = gameId, creator = "Вестерос", message = $"dynamic_errorStep*homeType_{user.HomeType}*{message}" }));
                        //GameHost.AddChatFunc(gameId, "Вестерос", string.Format("dynamic_errorStep*homeType_{0}*{1}", user.HomeType, message));
                    }
                }
                catch (Exception exp) { GameException.NewGameException(gameId, "Не удалось обработать результаты \"чит проверки\" (Failed to process the results of \"cheat validate\").", exp, false); }
            });
        }
    }

    public class TheEndException : Exception { }
}
