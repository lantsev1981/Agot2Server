using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    static public class ExtRandomDesk
    {
        static public Guid GetRandomCardId(this List<RandomDesk> o)
        {
            int rndIndex = GameHost.Rnd.Next(o.Count);

            RandomDesk result = o[rndIndex];
            result.PlayCount++;
            o.RemoveAt(rndIndex);

            return result.Id;
        }

        static public WCFRandomDesk ToWCFRandomDesk(this RandomDesk o)
        {
            return new WCFRandomDesk()
            {
                Id = o.Id,
                Strength = o.Strength,
                Attack = o.Attack,
                Defence = o.Defence,
                Skull = o.Skull,
                fileName = o.fileName
            };
        }
    }
}
