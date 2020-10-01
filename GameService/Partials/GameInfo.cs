using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class GameInfo
    {
        public WCFGameInfo ToWCFGameInfo()
        {
            WCFGameInfo result = new WCFGameInfo();
            result.Step = this.Step;
            result.Turn = this.Turn;
            result.Barbarian = this.Barbarian;

            result.Battle = this.Battle == null ? null : this.Battle.ToWCFBattle();

            result.Garrison = new List<WCFGarrison>();
            foreach (var item in this.Garrison)
                result.Garrison.Add(item.ToWCFGarrison());

            result.VesterosDecks = new List<WCFVesterosDecks>();
            foreach (var item in VesterosDecks.Where(p => p.IsFull))
                result.VesterosDecks.Add(item.ToWCFVesterosDecks());

            return result;
        }

        public void CopyGameInfo(Step step)
        {
            GameInfo result = new GameInfo();

            result.Step1 = step;

            result.Step = step.Id;
            result.Game = step.Game;
            result.Turn = this.Turn;
            result.Barbarian = this.Barbarian;

            foreach (var item in this.Garrison.ToList())
                item.CopyGarrison(result);

            foreach (var item in this.VesterosDecks.ToList())
                item.CopyVesterosDecks(result);

            if (this.Battle != null)
                this.Battle.CopyBattle(result);

            step.GameInfo = result;
        }        
    }
}
