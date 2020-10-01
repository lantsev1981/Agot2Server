using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GameService
{
    static public class ExtWCFGameException
    {
        static public GameException ToGameException(this WCFGameException o)
        {
            GameException result = new GameException();

            result.Game = o.GameId == Guid.Empty ? new Guid?() : o.GameId;
            result.Login = o.Login;
            result.Message = o.Message;

            return result;
        }
    }
}
