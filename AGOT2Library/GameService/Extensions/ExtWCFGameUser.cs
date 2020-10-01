using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static internal class ExtWCFGameUser
    {
        /// <summary>
        /// Проверяет входные данные
        /// </summary>
        /// <returns></returns>
        static internal bool CheckIn(this WCFGameUser o)
        {
            if (o.Id == Guid.Empty) 
                return false;
            if (o.Game == Guid.Empty)
                return false;
            if (string.IsNullOrEmpty(o.Login) || o.Login == "System" || o.Login == "Вестерос") 
                return false;

            return true;
        }

        static internal bool Check(this WCFGameUser o, GameUser serverUser)
        {
            if (o.Game != serverUser.Game)
                return false;
            if (o.Login != serverUser.Login)
                return false;
            if (o.HomeType != serverUser.HomeType)
                return false;

            return true;
        }
    }
}
