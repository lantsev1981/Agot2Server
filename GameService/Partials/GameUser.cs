using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


//using GamePortal;
//using Notifications;

namespace GameService
{
    public enum ChangeTrackEffect
    {
        First,
        Up,
        Down,
        Last
    }

    public partial class GameUser
    {

        Step _LastStep;
        public Step LastStep
        {
            get
            {
                return _LastStep == null
                    ? this.Step.Single(p => p.Id == this.Step.Max(p1 => p1.Id))
                    : _LastStep;
            }
            set { _LastStep = value; }
        }

        public GameUser(Game game)
            : this()
        {
            this.Game = game.Id;
            this.Id = Guid.NewGuid();
            this.Game1 = game;
            this.LastUpdate = DateTimeOffset.UtcNow;

            game.GameUser.Add(this);
        }

        public WCFGameUser ToWCFGameUser(GameUser serverUser = null, WCFGameUser clientUser = null)
        {
            WCFGameUser result = new WCFGameUser();
            result.Id = this.Id;
            result.Game = this.Game;
            result.Login = this.Login;
            result.HomeType = this.HomeType;
            result.NeedReConnect = this.NeedReConnect;

            if (serverUser != null)
                result.OnLineStatus = (serverUser.LastUpdate - this.LastUpdate) < new TimeSpan(0, 0, 5);

            if (clientUser != null)
            {
                result.NewStep = clientUser.LastStepIndex != this.Game1.StepIndex
                    ? true
                    : false;
            }

            return result;
        }

        public void CopyGameUser(Game game)
        {
            GameUser result = new GameUser(game);
            result.HomeType = this.HomeType;
            result.Login = this.Login == "Вестерос" ? "Вестерос" : null;

            Step newStep = this.LastStep.CopyStep("Default", true, result);

            if (result.HomeType != null)
                newStep.NewMessage("dynamic_newGame*homeType_" + result.HomeType);
        }

        public void NewSupport(Guid battleId)
        {
            Step newStep = this.LastStep.CopyStep("Подмога", false);
            newStep.NewMessage("dynamic_planning*stepType_Подмога");

            newStep.Support = new Support();
            newStep.Support.Step = newStep.Id;
            newStep.Support.Game = newStep.Game;
            newStep.Support.BattleId = battleId;
            newStep.Support.Step1 = newStep;
        }

        public void NewBattleUser(Guid battleId)
        {
            Step newStep = this.LastStep.CopyStep("Сражение", false);
            newStep.GetStrength();
            newStep.NewMessage("dynamic_homeCardSelect");
        }

        public void ChangeTrack(string symbolName, ChangeTrackEffect effect)
        {
            string trackName = string.Empty;
            switch (symbolName)
            {
                case "Железный_трон": trackName = "Железный_трон"; break;
                case "Валирийский_меч": trackName = "Вотчины"; break;
                case "Посыльный_ворон": trackName = "Королевский_двор"; break;
            }

            Step newStep = this.LastStep.CopyStep("Default", true);

            switch (symbolName)
            {
                case "Железный_трон":
                    switch (effect)
                    {
                        case ChangeTrackEffect.First:
                            newStep.GameUserInfo.ThroneInfluence = 1;
                            break;
                        case ChangeTrackEffect.Up:
                            newStep.GameUserInfo.ThroneInfluence--;
                            break;
                        case ChangeTrackEffect.Down:
                            if (newStep.GameUserInfo.ThroneInfluence < 6)
                                newStep.GameUserInfo.ThroneInfluence++;
                            break;
                        case ChangeTrackEffect.Last:
                            newStep.GameUserInfo.ThroneInfluence = 6;
                            break;
                    }
                    newStep.NewMessage(string.Format("dynamic_voting*voting_{0}*{1}", trackName, newStep.GameUserInfo.ThroneInfluence));
                    break;
                case "Валирийский_меч":
                    switch (effect)
                    {
                        case ChangeTrackEffect.First:
                            newStep.GameUserInfo.BladeInfluence = 1;
                            break;
                        case ChangeTrackEffect.Up:
                            newStep.GameUserInfo.BladeInfluence--;
                            break;
                        case ChangeTrackEffect.Down:
                            if (newStep.GameUserInfo.BladeInfluence < 6)
                                newStep.GameUserInfo.BladeInfluence++;
                            break;
                        case ChangeTrackEffect.Last:
                            newStep.GameUserInfo.BladeInfluence = 6;
                            break;
                    }
                    newStep.NewMessage(string.Format("dynamic_voting*voting_{0}*{1}", trackName, newStep.GameUserInfo.BladeInfluence));
                    break;
                case "Посыльный_ворон":
                    switch (effect)
                    {
                        case ChangeTrackEffect.First:
                            newStep.GameUserInfo.RavenInfluence = 1;
                            break;
                        case ChangeTrackEffect.Up:
                            newStep.GameUserInfo.RavenInfluence--;
                            break;
                        case ChangeTrackEffect.Down:
                            if (newStep.GameUserInfo.RavenInfluence < 6)
                                newStep.GameUserInfo.RavenInfluence++;
                            break;
                        case ChangeTrackEffect.Last:
                            newStep.GameUserInfo.RavenInfluence = 6;
                            break;
                    }
                    newStep.NewMessage(string.Format("dynamic_voting*voting_{0}*{1}", trackName, newStep.GameUserInfo.RavenInfluence));
                    break;
            }
        }
    }
}
