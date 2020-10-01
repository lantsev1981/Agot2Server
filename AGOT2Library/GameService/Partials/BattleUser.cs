using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameService
{
    public partial class BattleUser
    {
        internal WCFBattleUser ToWCFBattleUser()
        {
            Game game = this.Step1.GameUser1.Game1;

            WCFBattleUser result = new WCFBattleUser();
            result.Step = this.Step;
            result.BattleId = this.BattleId;
            result.AdditionalEffect = this.AdditionalEffect;

            List<Step> lastHomeSteps = game.LastHomeSteps;
            if (!string.IsNullOrEmpty(this.AdditionalEffect) || lastHomeSteps.Where(p => p.StepType == "Сражение").All(p => p.IsFull))
                result.HomeCardType = this.HomeCardType;

            result.Strength = this.Strength;
            result.IsWinner = this.IsWinner;

            if (this.IsWinner.HasValue || game.CurrentUser == this.Step1.GameUser1)
                result.RandomDeskId = this.RandomDeskId;

            return result;
        }

        internal void CopyBattleUser(Step step)
        {
            BattleUser result = new BattleUser();

            result.Step1 = step;

            result.Step = step.Id;
            result.Game = step.Game;
            result.BattleId = this.BattleId;
            result.HomeCardType = this.HomeCardType;
            result.AdditionalEffect = this.AdditionalEffect;
            result.IsWinner = this.IsWinner;
            result.RandomDeskId = this.RandomDeskId;
            result.Strength = this.Strength;

            step.BattleUser = result;
        }

        internal HomeCardType LocalHomeCardType
        {
            get { return !string.IsNullOrEmpty(this.HomeCardType) ? this.Step1.GameUser1.Game1.DbContext.HomeCardType.Single(p => p.Name == this.HomeCardType) : null; }
        }

        internal RandomDesk LocalRandomCard
        {
            get { return this.RandomDeskId.HasValue ? this.Step1.GameUser1.Game1.DbContext.RandomDesk.Single(p => p.Id == this.RandomDeskId) : null; }
        }
    }
}
