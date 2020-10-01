using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace GameService
{
    public partial class GameUserInfo
    {
        public WCFGameUserInfo ToWCFGameUserInfo()
        {
            WCFGameUserInfo result = new WCFGameUserInfo();
            result.Step = this.Step;
            result.Power = this.Power;
            result.Supply = this.Supply;
            result.ThroneInfluence = this.ThroneInfluence;
            result.BladeInfluence = this.BladeInfluence;
            result.RavenInfluence = this.RavenInfluence;
            result.IsBladeUse = this.IsBladeUse;

            result.GameUserTerrain = new List<WCFGameUserTerrain>();
            foreach (var item in this.GameUserTerrain)
                result.GameUserTerrain.Add(item.ToWCFGameUserTerrain());

            result.Order = new List<WCFOrder>();
            foreach (var item in this.Order)
                result.Order.Add(item.ToWCFOrder());

            result.PowerCounter = new List<WCFPowerCounter>();
            foreach (var item in this.PowerCounter)
                result.PowerCounter.Add(item.ToWCFPowerCounter());

            result.Unit = new List<WCFUnit>();
            foreach (var item in this.Unit)
                result.Unit.Add(item.ToWCFUnit());

            result.UsedHomeCard = new List<WCFUsedHomeCard>();
            foreach (var item in this.UsedHomeCard)
                result.UsedHomeCard.Add(item.ToWCFUsedHomeCard());

            return result;
        }

        public void CopyGameUserInfo(Step step)
        {
            GameUserInfo result = new GameUserInfo();

            result.Step1 = step;

            result.Step = step.Id;
            result.Game = step.Game;
            result.Power = this.Power;
            result.Supply = this.Supply;
            result.ThroneInfluence = this.ThroneInfluence;
            result.BladeInfluence = this.BladeInfluence;
            result.RavenInfluence = this.RavenInfluence;
            result.IsBladeUse = this.IsBladeUse;

            foreach (var item in this.GameUserTerrain.ToList())
                item.CopyGameUserTerrain(result);
            foreach (var item in this.PowerCounter.ToList())
                item.CopyPowerCounter(result);
            foreach (var item in this.Unit.ToList())
                item.CopyUnit(result);
            foreach (var item in this.Order.ToList())
                item.CopyOrder(result);
            foreach (var item in this.UsedHomeCard.ToList())
                item.CopyUsedHomeCard(result);

            step.GameUserInfo = result;
        }

        public void NewThink()
        {
            this.Order.Clear();
            foreach (var terrain in this.Unit.Select(p => p.Terrain1).Distinct().ToList())
            {
                Order order = new Order();
                order.Id = Guid.NewGuid();
                order.Step = this.Step;
                order.Game = this.Game;
                order.FirstId = order.Id;
                order.Terrain = terrain.Name;
                order.Terrain1 = terrain;
                order.GameUserInfo = this;

                this.Order.Add(order);
            }
        }

        public void NewGameUserTerrain(Terrain terrain)
        {
            this.GameUserTerrain.Add(
                new GameUserTerrain()
                {
                    Id = Guid.NewGuid(),
                    Step = this.Step,
                    Game = this.Game,
                    GameUserInfo = this,
                    Terrain = terrain.Name,
                    Terrain1 = terrain
                });
        }

        /// <summary>
        /// Изменяет количество доступной власти
        /// </summary>
        /// <param name="powerCount">определяет число на которое будет изменено значение доступной власти</param>
        /// <returns>возвращает сообщение до(при достижении предела)/на сколько изменилось значение доступной власти</returns>
        public void ChangePower(int powerCount)
        {
            if (Power + powerCount < 0)
                Power = 0;
            else if (PowerCounter.Count + Power + powerCount > 20)
                Power = 20 - PowerCounter.Count;
            else Power += powerCount;
        }

        public void NewPowerCounter(Terrain terrain)
        {
            PowerCounter result = new PowerCounter()
            {
                Terrain = terrain.Name,
                Terrain1 = terrain,
                TokenType = "Жетон_власти",
                Game = this.Game,
                GameUserInfo = this,
                Id = Guid.NewGuid(),
                Step = this.Step
            };
            this.PowerCounter.Add(result);
        }
    }
}
