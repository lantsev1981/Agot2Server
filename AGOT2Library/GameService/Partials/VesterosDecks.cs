using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class VesterosDecks
    {
        public VesterosDecks() { }

        public VesterosDecks(VesterosCardType cardType)
            :this()
        {
            this.Id = Guid.NewGuid();
            this.FirstId = this.Id;
            this.VesterosCardType = cardType.Id;
            this.VesterosCardType1 = cardType;
        }

        internal void CopyVesterosDecks(GameInfo gameInfo)
        {
            VesterosDecks result = new VesterosDecks(this.VesterosCardType1);
            result.Step = gameInfo.Step;
            result.Game = gameInfo.Game;
            result.GameInfo = gameInfo;
            result.VesterosActionType = this.VesterosActionType;
            result.VesterosActionType1 = this.VesterosActionType1;

            result.FirstId = this.FirstId;
            result.IsFull = this.IsFull;
            result.Sort = this.Sort;

            gameInfo.VesterosDecks.Add(result);
        }

        internal WCFVesterosDecks ToWCFVesterosDecks()
        {
            WCFVesterosDecks result = new WCFVesterosDecks();
            result.Id = this.FirstId;
            result.IsFull = this.IsFull;
            result.Sort = this.Sort;
            result.Step = this.Step;
            result.VesterosActionType = this.VesterosActionType;
            result.VesterosCardType = this.VesterosCardType;

            return result;
        }
    }
}
