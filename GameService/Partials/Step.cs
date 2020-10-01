using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace GameService
{
    public partial class Step
    {
        public bool IsNew { get; set; }

        public int CastleCount { get { return this.GameUserInfo.GameUserTerrain.Count(p1 => p1.Terrain1.Strength > 0); } }

        public int CastlePower { get { return this.GameUserInfo.GameUserTerrain.Sum(p1 => p1.Terrain1.Strength); } }

        public WCFStep ToWCFStep()
        {
            WCFStep result = new WCFStep();
            result.Id = this.Id;
            result.Game = this.Game;
            result.StepType = this.StepType;
            result.IsFull = this.IsFull;
            result.GameUser = this.GameUser;

            result.Message = new List<string>();
            foreach (var item in this.Message)
                result.Message.Add(item.Value);

            result.GameInfo = this.GameInfo == null ? null : this.GameInfo.ToWCFGameInfo();
            result.GameUserInfo = this.GameUserInfo == null ? null : this.GameUserInfo.ToWCFGameUserInfo();

            result.Raven = this.Raven == null ? null : this.Raven.ToWCFRaven();
            result.BattleUser = this.BattleUser == null ? null : this.BattleUser.ToWCFBattleUser();
            result.Support = this.Support == null ? null : this.Support.ToWCFSupport();
            result.Raid = this.Raid == null ? null : this.Raid.ToWCFRaid();
            result.March = this.March == null ? null : this.March.ToWCFMarch();
            result.Voting = this.Voting == null ? null : this.Voting.ToWCFVoting();
            result.VesterosAction = this.VesterosAction == null ? null : this.VesterosAction.ToWCFVesterosAction();
            result.ArrowModelList = this.Arrow.ToList().Select(p => p.ToArrowModel()).ToList();

            return result;
        }

        /// <summary>
        /// Создаёт новый ход копируя старый
        /// </summary>
        /// <param name="holderUser">для кого создаётся ход (null - владелец не меняется)</param>
        /// <returns></returns>
        public Step CopyStep(string stepType, bool isFull, GameUser holderUser = null)
        {
            holderUser = holderUser == null
                ? this.GameUser1
                : holderUser;

            Step result = this.IsNew ? holderUser.LastStep : new Step() { StepType = stepType, IsFull = isFull };

            if (result.IsFull)
            {
                result.StepType = stepType != "Default" || string.IsNullOrEmpty(this.GameUser1.HomeType)
                    ? stepType
                    : this.GameUser1.Game1.LastStep.StepType;

                result.IsFull = isFull;
            }

            if (!IsNew)
            {
                result.GameUser1 = holderUser;

                holderUser.LastUpdate = DateTimeOffset.UtcNow;//исключает завершение партии при создании хода давно ушедшему игроку
                holderUser.Game1.StepIndex++;
                result.Id = holderUser.Game1.StepIndex;
                result.IsNew = true;
                result.Game = holderUser.Game1.Id;
                result.GameUser = holderUser.Id;

                if (holderUser.Login == "Вестерос")
                    this.GameUser1.Game1.GameInfo.CopyGameInfo(result);
                else
                    this.GameUserInfo.CopyGameUserInfo(result);

                if (this.Support != null)
                    this.Support.CopySupport(result);
                if (this.BattleUser != null)
                    this.BattleUser.CopyBattleUser(result);
                if (this.Voting != null)
                    this.Voting.CopyTo(result);
                if (this.Raven != null)
                    this.Raven.CopyRaven(result);

                //в конце что бы избежать копирования GameInfo из себя
                result.GameUser1.Step.Add(result);
                result.GameUser1.LastStep = result;
                result.GameUser1.Game1.LastStep = result;
            }

            return result;
        }

        public void NewMessage(string value)
        {
            Game game = this.GameUser1.Game1;

            Message result = new Message();
            result.Step1 = this;

            game.MessageIndex++;
            result.Id = game.MessageIndex;
            result.Game = game.Id;
            result.Step = this.Id;
            result.Value = value;

            result.Step1.Message.Add(result);
        }

        public bool _IsMath;
        public bool IsSupport = false;
        public int GetStrength(bool isForce = false)
        {
            if (!isForce && _IsMath)
                return this.BattleUser.Strength.Value;
            this.BattleUser.Strength = 0;

            //Меч
            if (this.Raven != null && this.Raven.StepType == "Валирийский_меч")
                this.BattleUser.Strength++;

            GameUser currentUser = this.GameUser1;
            Game game = currentUser.Game1;
            Battle battle = game.GameInfo.Battle;
            bool isAttackHome = battle.AttackUser == currentUser.Id;
            Terrain battleTerrain = battle.LocalDefenceTerrain;
            GameUser opponentUser = isAttackHome
                ? battle.LocalDefenceUser
                : battle.LocalAttackUser;


            //карты перевеса
            RandomDesk randomCard = this.BattleUser.LocalRandomCard;
            if (randomCard != null && randomCard.Strength > 0)
            {
                this.BattleUser.Strength += randomCard.Strength;
            }

            //Сила карты дома
            if (!string.IsNullOrEmpty(this.BattleUser.HomeCardType))
            {
                if (!BalonGreyjoy(opponentUser))
                    this.BattleUser.Strength += this.BattleUser.LocalHomeCardType.Strength;
                StannisBaratheon(opponentUser);
                SerDavosSeaworth();
                TheonGreyjoy(isAttackHome, battleTerrain);
                dragon_Ramsay_Bolton();
                dragon_Doran_Martell();
                dragon_Quentyn_Martell();
                dragon_Euron_Crows_Eye(opponentUser);
                dragon_Aeron_Damphair();
            }

            //Сила гарнизона            
            if (!isAttackHome)
            {
                Garrison garrison = game.GameInfo.Garrison.SingleOrDefault(p => p.Terrain == battleTerrain.Name);
                if (garrison != null) this.BattleUser.Strength += garrison.Strength;
            }

            //сила приказа
            Order sourceOrder = this.GameUserInfo.Order.SingleOrDefault(p => p.Terrain == (isAttackHome ? battle.AttackTerrain : battle.DefenceTerrain));
            if (sourceOrder != null && ((isAttackHome && sourceOrder.OrderType.Contains("Поход"))
                || (!isAttackHome && sourceOrder.OrderType.IndexOf("Оборона") != -1)))
            {
                OrderType ordertType = game.DbContext.OrderType.Single(p => p.Name == sourceOrder.OrderType);
                this.BattleUser.Strength += ordertType.Strength;
                CatelynStark(sourceOrder, ordertType);
            }

            //сила юнитов
            this.BattleUser.Strength += GetUnitStrength(this.GameUserInfo.Unit.Where(p => p.Terrain == battleTerrain.Name).ToList(), isAttackHome, battleTerrain, opponentUser);

            //Сила поддержки
            foreach (var joinTerrain in battleTerrain.TerrainTerrain.Select(p => p.Terrain2))
            {
                //проверка лояльности
                GameUser holder = game.GetTerrainHolder(joinTerrain);
                if (holder == null || holder.LastStep.Support == null || holder.LastStep.Support.BattleId != battle.Id || holder.LastStep.Support.SupportUser != currentUser.Id)
                    continue;

                //проверка наличия приказа поддержки
                Order supportOrder = holder.LastStep.GameUserInfo.Order.SingleOrDefault(p => p.Terrain == joinTerrain.Name);
                if (supportOrder == null || supportOrder.OrderType.IndexOf("Подмога") == -1)
                    continue;
                //поддержка с земли на воду не оказывается
                if (joinTerrain.TerrainType == "Земля" && battleTerrain.TerrainType != "Земля")
                    continue;
                //поддержка с порта на землю не оказывается
                if (joinTerrain.TerrainType == "Порт" && battleTerrain.TerrainType == "Земля")
                    continue;

                IsSupport = true;

                //подсчёт силы приказа и юнитов
                OrderType ordertType = game.DbContext.OrderType.Single(p => p.Name == supportOrder.OrderType);
                int strength = ordertType.Strength;
                strength += GetUnitStrength(holder.LastStep.GameUserInfo.Unit.Where(p => p.Terrain == joinTerrain.Name).ToList(), isAttackHome, battleTerrain, opponentUser);

                this.BattleUser.Strength += strength;
            }

            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "dragon_Mance_Rayder")
                this.BattleUser.Strength = this.GameUser1.Game1.GameInfo.Barbarian;

            _IsMath = true;
            return this.BattleUser.Strength.Value;
        }

        //сила юнитов
        private int GetUnitStrength(List<Unit> unitList, bool isAttackHome, Terrain battleTerrain, GameUser opponentUser)
        {
            int result = 0;
            foreach (var item in unitList)
            {
                if (item.IsWounded)
                    continue;
                if (item.UnitType == "Осадная_башня"
                    && (!isAttackHome || battleTerrain.Strength == 0))
                    continue;

                if (this.BattleUser != null
                    && this.BattleUser.AdditionalEffect != "Block"
                    && this.BattleUser.HomeCardType == "Ренли_Баратеон"
                    && item.UnitType == "Пеший_воин")
                    if (item.GameUserInfo.Step1 == this)
                        this.BattleUser.AdditionalEffect += item.FirstId + "|";

                if (SalladhorSaan(opponentUser, item))
                    continue;

                result += item.UnitType1.Strength;

                if (SerKevanLannister(isAttackHome, item) || VictarionGreyjoy(isAttackHome, item) || dragon_Paxter_Redwyne(battleTerrain, item) || dragon_Ser_Addam_Marbrand(isAttackHome, item))
                    result += 1;
            }
            return result;
        }

        private void dragon_Aeron_Damphair()
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "dragon_Aeron_Damphair")
            {
                int value = 0;
                int.TryParse(this.BattleUser.AdditionalEffect, out value);
                if (value != 0)
                {
                    this.BattleUser.Strength += value;
                    this.NewMessage(string.Format("dynamic_heroRage*hero_dragon_Aeron_Damphair*rageEffect_31*{0}", value));
                }
            }
        }

        private void dragon_Euron_Crows_Eye(GameUser opponentUser)
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "dragon_Euron_Crows_Eye"
                && opponentUser.LastStep.GameUserInfo.BladeInfluence < this.GameUserInfo.BladeInfluence)
            {
                this.BattleUser.Strength++;
                this.NewMessage("dynamic_heroRage*hero_dragon_Euron_Crows_Eye*rageEffect_9");
            }
        }

        private void dragon_Ramsay_Bolton()
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "dragon_Ramsay_Bolton"
                && this.GameUserInfo.UsedHomeCard.All(p => p.HomeCardType != "dragon_Reek"))
            {
                this.BattleUser.Strength++;
                this.NewMessage("dynamic_heroRage*hero_dragon_Ramsay_Bolton*rageEffect_9");
            }
        }

        private void dragon_Doran_Martell()
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "dragon_Doran_Martell")
            {
                this.BattleUser.Strength -= 6 - this.GameUserInfo.UsedHomeCard.Count();
                if (this.BattleUser.Strength < 0)
                    this.BattleUser.Strength = 0;
                this.NewMessage(string.Format("dynamic_heroRage*hero_dragon_Doran_Martell*rageEffect_31*{0}", this.BattleUser.Strength));
            }
        }

        private void dragon_Quentyn_Martell()
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "dragon_Quentyn_Martell")
            {
                this.BattleUser.Strength += this.GameUserInfo.UsedHomeCard.Count();
                this.NewMessage(string.Format("dynamic_heroRage*hero_dragon_Quentyn_Martell*rageEffect_31*{0}", this.BattleUser.Strength));
            }
        }

        private bool dragon_Paxter_Redwyne(Terrain battleTerrain, Unit item)
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "dragon_Paxter_Redwyne"
                && battleTerrain.TerrainType == "Море"
                && item.UnitType == "Корабль"
                && item.Step == this.Id)//только кораблей дома
            {
                this.NewMessage("dynamic_heroRage*hero_dragon_Paxter_Redwyne*rageEffect_12");
                return true;
            }

            return false;
        }

        private bool dragon_Ser_Addam_Marbrand(bool isAttackHome, Unit item)
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "dragon_Ser_Addam_Marbrand"
                && isAttackHome
                && item.UnitType == "Рыцарь"
                && item.Step == this.Id)//только Лани
            {
                this.NewMessage("dynamic_heroRage*hero_dragon_Ser_Addam_Marbrand*rageEffect_28");
                return true;
            }

            return false;
        }

        private void SerDavosSeaworth()
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "Сер_Давос_Сиворт"
                && this.GameUserInfo.UsedHomeCard.Any(p => p.HomeCardType == "Станис_Баратеон"))
            {
                this.BattleUser.Strength++;
                this.NewMessage("dynamic_heroRage*hero_Сер_Давос_Сиворт*rageEffect_9");
            }
        }

        private void StannisBaratheon(GameUser opponentUser)
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "Станис_Баратеон"
                && opponentUser.LastStep.GameUserInfo.ThroneInfluence < this.GameUserInfo.ThroneInfluence)
            {
                this.BattleUser.Strength++;
                this.NewMessage("dynamic_heroRage*hero_Станис_Баратеон*rageEffect_9");
            }
        }

        private bool VictarionGreyjoy(bool isAttackHome, Unit item)
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "Виктарион_Грейджой"
                && isAttackHome
                && item.UnitType == "Корабль"
                && item.Step == this.Id)//только кораблей Греев
            {
                this.NewMessage("dynamic_heroRage*hero_Виктарион_Грейджой*rageEffect_12");
                return true;
            }

            return false;
        }

        private bool SerKevanLannister(bool isAttackHome, Unit item)
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "Сер_Киван_Ланнистер"
                && isAttackHome
                && item.UnitType == "Пеший_воин"
                && item.Step == this.Id)//только пехи Лани
            {
                this.NewMessage("dynamic_heroRage*hero_Сер_Киван_Ланнистер*rageEffect_13");
                return true;
            }

            return false;
        }

        private bool SalladhorSaan(GameUser opponentUser, Unit item)
        {
            //отменяет силу чужих кораблей
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "Салладор_Саан"
                && this.IsSupport
                && item.UnitType == "Корабль"
                && item.Step != this.Id)
            {
                this.NewMessage("dynamic_heroRage*hero_Салладор_Саан*rageEffect_14");
                return true;
            }

            //отменяет силу всех кораблей противника
            if (opponentUser.LastStep.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && opponentUser.LastStep.BattleUser.HomeCardType == "Салладор_Саан"
                && opponentUser.LastStep.IsSupport
                && item.UnitType == "Корабль")
            {
                this.NewMessage("dynamic_heroRage*hero_Салладор_Саан*rageEffect_14");
                return true;
            }

            return false;
        }

        private void TheonGreyjoy(bool isAttackHome, Terrain battleTerrain)
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "Теон_Грейджой"
                && !isAttackHome
                && battleTerrain.Strength != 0)
            {
                this.BattleUser.Strength++;
                this.NewMessage("dynamic_heroRage*hero_Теон_Грейджой*rageEffect_9");
            }
        }

        private void CatelynStark(Order sourceOrder, OrderType ordertType)
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && this.BattleUser.HomeCardType == "Кейтилин_Старк"
                && sourceOrder.OrderType.IndexOf("Оборона") != -1)
            {
                this.BattleUser.Strength += ordertType.Strength;
                this.NewMessage("dynamic_heroRage*hero_Кейтилин_Старк*rageEffect_10");
            }
        }

        private bool BalonGreyjoy(GameUser opponentUser)
        {
            if (this.BattleUser != null
                && this.BattleUser.AdditionalEffect != "Block"
                && opponentUser.LastStep.BattleUser.HomeCardType == "Бейлон_Грейджой")
            {
                this.NewMessage("dynamic_heroRage*hero_Бейлон_Грейджой*rageEffect_11");
                return true;
            }

            return false;
        }

        public int CheckSupply(Terrain exceptTerrain = null)
        {
            //снабжение
            List<int> supplyArmy = null;
            switch (this.GameUserInfo.Supply)
            {
                case 0:
                    supplyArmy = new List<int>() { 2, 2 };
                    break;
                case 1:
                    supplyArmy = new List<int>() { 2, 3 };
                    break;
                case 2:
                    supplyArmy = new List<int>() { 2, 2, 3 };
                    break;
                case 3:
                    supplyArmy = new List<int>() { 2, 2, 2, 3 };
                    break;
                case 4:
                    supplyArmy = new List<int>() { 2, 2, 3, 3 };
                    break;
                case 5:
                    supplyArmy = new List<int>() { 2, 2, 3, 4 };
                    break;
                case 6:
                    supplyArmy = new List<int>() { 2, 2, 2, 3, 4 };
                    break;
            }


            List<Terrain> armyTerrain = this.GameUserInfo.Unit.Select(p => p.Terrain1).Distinct().ToList();
            if (exceptTerrain != null)
                armyTerrain.Remove(exceptTerrain);
            armyTerrain = armyTerrain.OrderByDescending(p => this.GameUserInfo.Unit.Count(p1 => p1.Terrain1 == p)).ThenBy(p => p.TerrainType).ToList();

            var result = 0;
            foreach (var item in armyTerrain)
            {
                int unitCount = this.GameUserInfo.Unit.Count(p => p.Terrain1 == item);
                if (unitCount < 2)
                    break;

                //добавляем потери если не вмещаемся в порт
                if (unitCount > 3 && item.TerrainType == "Порт")
                {
                    result++;
                    unitCount--;
                }

                if (supplyArmy.Count == 0)
                    result += unitCount - 1;
                else
                {
                    int supply = supplyArmy.FirstOrDefault(p => (p - unitCount) >= 0);
                    if (supply == 0)
                    {
                        supply = supplyArmy.LastOrDefault();
                        result += unitCount - supply;
                    }

                    supplyArmy.Remove(supply);
                }
            }

            return result;
        }

        public void NewSupplyStep()
        {
            Step newStep = this.CopyStep("Роспуск_войск", false);
            newStep.NewMarch();
            newStep.March.SourceOrder = "Снабжение_войск";
            foreach (var unit in newStep.GameUserInfo.Unit)
            {
                newStep.March.MarchUnit.Add(new MarchUnit()
                {
                    Id = Guid.NewGuid(),
                    March = newStep.March,
                    Step = newStep.March.Step,
                    Terrain = unit.Terrain,
                    Unit = unit.FirstId,
                    UnitType = unit.UnitType
                });
            }
            newStep.NewMessage("dynamic_supplyLimit");
        }

        internal void NewRaven()
        {
            Raven result = new Raven();
            result.Step1 = this;
            result.Step = this.Id;
            result.Game = this.Game;
            this.Raven = result;
        }

        public void NewMarch()
        {
            March result = new March();
            result.Step1 = this;
            result.Step = this.Id;
            result.Game = this.Game;
            this.March = result;
        }
    }
}
