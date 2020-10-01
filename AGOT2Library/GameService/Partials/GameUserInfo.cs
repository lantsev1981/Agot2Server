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
            WCFGameUserInfo result = new WCFGameUserInfo
            {
                Step = this.Step,
                Power = this.Power,
                Supply = this.Supply,
                ThroneInfluence = this.ThroneInfluence,
                BladeInfluence = this.BladeInfluence,
                RavenInfluence = this.RavenInfluence,
                IsBladeUse = this.IsBladeUse,

                GameUserTerrain = new List<WCFGameUserTerrain>()
            };

            foreach (GameUserTerrain item in this.GameUserTerrain)
                result.GameUserTerrain.Add(item.ToWCFGameUserTerrain());

            result.Order = new List<WCFOrder>();
            foreach (Order item in this.Order)
                result.Order.Add(item.ToWCFOrder());

            result.PowerCounter = new List<WCFPowerCounter>();
            foreach (PowerCounter item in this.PowerCounter)
                result.PowerCounter.Add(item.ToWCFPowerCounter());

            result.Unit = new List<WCFUnit>();
            foreach (Unit item in this.Unit)
                result.Unit.Add(item.ToWCFUnit());

            result.UsedHomeCard = new List<WCFUsedHomeCard>();
            foreach (UsedHomeCard item in this.UsedHomeCard)
                result.UsedHomeCard.Add(item.ToWCFUsedHomeCard());

            return result;
        }

        public void CopyGameUserInfo(Step step)
        {
            GameUserInfo result = new GameUserInfo
            {
                Step1 = step,

                Step = step.Id,
                Game = step.Game,
                Power = this.Power,
                Supply = this.Supply,
                ThroneInfluence = this.ThroneInfluence,
                BladeInfluence = this.BladeInfluence,
                RavenInfluence = this.RavenInfluence,
                IsBladeUse = this.IsBladeUse
            };

            foreach (GameUserTerrain item in this.GameUserTerrain.ToList())
                item.CopyGameUserTerrain(result);
            foreach (PowerCounter item in this.PowerCounter.ToList())
                item.CopyPowerCounter(result);
            foreach (Unit item in this.Unit.ToList())
                item.CopyUnit(result);
            foreach (Order item in this.Order.ToList())
                item.CopyOrder(result);
            foreach (UsedHomeCard item in this.UsedHomeCard.ToList())
                item.CopyUsedHomeCard(result);

            step.GameUserInfo = result;
        }

        public void NewThink()
        {
            this.Order.Clear();
            foreach (Terrain terrain in this.Unit.Select(p => p.Terrain1).Distinct().ToList())
            {
                Order order = new Order
                {
                    Id = Guid.NewGuid(),
                    Step = this.Step,
                    Game = this.Game,
                    Terrain = terrain.Name,
                    Terrain1 = terrain,
                    GameUserInfo = this
                };
                order.FirstId = order.Id;

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
