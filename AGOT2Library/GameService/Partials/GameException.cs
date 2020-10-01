using Agot2Server;
using ChatServer;
using GameService;
using System;
using System.Linq;

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

        public static void NewGameException(Guid? gameId, string specialText, Exception exp, bool sendMsg)
        {
            GameException result = new GameException
            {
                Game = gameId,
                Message = $"{GameException.CombineMsg(exp, specialText)}",
                stackTrace = exp.StackTrace
            };

            using (Agot2p6Entities agot2p6Entities = new Agot2p6Entities())
            {
                agot2p6Entities.GameException.Add(result);
                agot2p6Entities.SaveChanges();
            }

            if (sendMsg)
            {
                lock (Service.WebSockServiceList)
                {
                    ChatService item = Service.WebSockServiceList.SingleOrDefault(p => p.GameId == gameId) as ChatService;
                    item?.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_error*error_criticalError*!\n{result.Message}" });
                }
            }
        }

        public static string CombineMsg(Exception exp, string msg)
        {
            msg += $"\n{exp.Message}";
            return exp.InnerException != null ? CombineMsg(exp.InnerException, msg) : msg;
        }
    }

    public class AntiCheatException : Exception
    {
        public AntiCheatException(Guid userId, string message)
        {
            Guid gameId = Guid.Empty;
            try
            {
                using (Agot2p6Entities agot2p6Entities = new Agot2p6Entities())
                {
                    GameUser user = agot2p6Entities.GameUser.Single(p => p.Id == userId);
                    gameId = user.Game;
                    user.NeedReConnect = true;
                    agot2p6Entities.SaveChanges();

                    lock (Service.WebSockServiceList)
                    {
                        ChatService item = Service.WebSockServiceList.SingleOrDefault(p => p.GameId == gameId) as ChatService;
                        item?.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_errorStep*homeType_{user.HomeType}*{message}" });
                    }
                }
            }
            catch (Exception exp) { GameException.NewGameException(gameId, "Не удалось обработать результаты \"чит проверки\" (Failed to process the results of \"cheat validate\").", exp, false); }
        }
    }

    public class TheEndException : Exception { }
}
