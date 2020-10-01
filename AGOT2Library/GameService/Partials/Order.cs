using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class Order
    {
        public WCFOrder ToWCFOrder()
        {
            WCFOrder result = new WCFOrder();
            result.Id = this.FirstId;
            result.Step = this.Step;
            if (this.GameUserInfo.Step1.GameUser1.Game1.LastHomeSteps.Where(p => p.StepType == "Замысел").All(p => p.IsFull))
                result.OrderType = this.OrderType;
            result.Terrain = this.Terrain;
            result.Step = this.Step;

            return result;
        }

        public void CopyOrder(GameUserInfo gameUserInfo)
        {
            Order result = new Order();

            result.GameUserInfo = gameUserInfo;
            result.Terrain1 = this.Terrain1;
            result.OrderType1 = this.OrderType1;

            result.Id = Guid.NewGuid();
            result.Step = gameUserInfo.Step;
            result.Game = gameUserInfo.Game;
            result.FirstId = this.FirstId;
            result.OrderType = this.OrderType;
            result.Terrain = this.Terrain;

            gameUserInfo.Order.Add(result);
        }

        public void СollectConsolidate(GameUser collectUser)
        {
            //если приказ свой
            if (this.GameUserInfo.Step1 == collectUser.LastStep)
            {
                int powerCount = this.Terrain1.ConsolidatePower(collectUser.Game1) + 1;
                GameUserInfo.ChangePower(powerCount);
                collectUser.LastStep.NewMessage(string.Format("dynamic_consolidatePower*terrain_{0}*{1}", this.Terrain, GameUserInfo.Power));
                this.GameUserInfo.Order.Remove(this);
            }
            else
            {
                collectUser.LastStep.GameUserInfo.ChangePower(1);
                collectUser.LastStep.NewMessage(string.Format("dynamic_consolidatePower*terrain_{0}*{1}", this.Terrain, GameUserInfo.Power));

                Step newStep = this.GameUserInfo.Step1.CopyStep("Default", true);
                newStep.GameUserInfo.ChangePower(-1);
                newStep.NewMessage(string.Format("dynamic_consolidatePowerLoses*terrain_{0}*{1}", this.Terrain, GameUserInfo.Power));
                newStep.GameUserInfo.Order.Remove(newStep.GameUserInfo.Order.Single(p => p.FirstId == this.FirstId));
            }
        }
    }
}
