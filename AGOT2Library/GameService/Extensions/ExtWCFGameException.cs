using System;

namespace GameService
{
    public static class ExtWCFGameException
    {
        public static GameException ToGameException(this WCFGameException o)
        {
            GameException result = new GameException
            {
                Game = string.IsNullOrWhiteSpace(o.Game) ? new Guid?() : Guid.Parse(o.Game),
                Login = o.Login,
                Message = o.Message,
                stackTrace = o.stackTrace
            };

            return result;
        }
    }
}
