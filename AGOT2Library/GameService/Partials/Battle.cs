using System;
using System.Collections.Generic;
using System.Linq;



namespace GameService
{
    public partial class Battle
    {
        public GameUser LocalDefenceUser => this.GameInfo.Step1.GameUser1.Game1.GameUser.Single(p => p.Id == this.DefenceUser);
        public GameUser LocalAttackUser => this.GameInfo.Step1.GameUser1.Game1.GameUser.Single(p => p.Id == this.AttackUser);
        public Terrain LocalDefenceTerrain => this.GameInfo.Step1.GameUser1.Game1.DbContext.Terrain.Single(p => p.Name == this.DefenceTerrain);
        public Terrain LocalAttackTerrain => this.GameInfo.Step1.GameUser1.Game1.DbContext.Terrain.Single(p => p.Name == this.AttackTerrain);

        private List<GameUser> _BattleUsers;
        public List<GameUser> BattleUsers
        {
            get
            {
                if (_BattleUsers == null)
                    _BattleUsers = new List<GameUser>() { this.LocalAttackUser, this.LocalDefenceUser };
                return _BattleUsers;
            }
        }
        public GameUser WinnerUser => BattleUsers.Single(p => p.LastStep.BattleUser.IsWinner == true);
        public GameUser LoserUser => BattleUsers.Single(p => p.LastStep.BattleUser.IsWinner == false);
        private Game _Game => this.GameInfo.Step1.GameUser1.Game1;

        public WCFBattle ToWCFBattle()
        {
            WCFBattle result = new WCFBattle
            {
                Id = this.Id,
                Step = this.Step,
                DefenceTerrain = this.DefenceTerrain,
                AttackTerrain = this.AttackTerrain,
                AttackUser = this.AttackUser,
                DefenceUser = this.DefenceUser,
                IsAttackUserNeedSupport = this.IsAttackUserNeedSupport,
                IsDefenceUserNeedSupport = this.IsDefenceUserNeedSupport
            };

            return result;
        }

        internal void CopyBattle(GameInfo gameInfo)
        {
            Battle result = new Battle
            {
                GameInfo = gameInfo,

                Id = this.Id,
                Step = gameInfo.Step,
                Game = gameInfo.Game,
                DefenceTerrain = this.DefenceTerrain,
                AttackTerrain = this.AttackTerrain,
                AttackUser = this.AttackUser,
                DefenceUser = this.DefenceUser,
                IsAttackUserNeedSupport = this.IsAttackUserNeedSupport,
                IsDefenceUserNeedSupport = this.IsDefenceUserNeedSupport
            };

            gameInfo.Battle = result;
        }

        public void Start()
        {
            //Поддержка
            IEnumerable<Order> supportOrder = _Game.LastHomeSteps.SelectMany(p => p.GameUserInfo.Order);
            supportOrder = supportOrder.Where(o => o.OrderType.Contains("Подмога"));
            supportOrder = supportOrder.Where(o => o.Terrain1.TerrainTerrain.Any(tt => tt.Terrain2 == this.LocalDefenceTerrain));
            supportOrder = supportOrder.Where(o => this.LocalDefenceTerrain.TerrainType == "Море" ? o.Terrain1.TerrainType != "Земля" : o.Terrain1.TerrainType != "Порт");

            List<GameUser> supportUser = supportOrder.Select(p => p.GameUserInfo.Step1.GameUser1).Distinct().ToList();

            if (IsDefenceUserNeedSupport == null && supportUser.Count() > 0)
            {
                Step newStep = this.LocalAttackUser.LastStep.CopyStep("Default", true);
                newStep.NewMessage(IsAttackUserNeedSupport == true ? "dynamic_RequestSupport4" : "dynamic_RequestSupport3");
                newStep.Raven = null;//Использование предметов обнуляем (меч и карты)
                newStep.BattleUser = new BattleUser
                {
                    Step = newStep.Id,
                    Game = newStep.Game,
                    BattleId = this.Id,
                    Step1 = newStep
                };

                newStep = this.LocalDefenceUser.LastStep.CopyStep("RequestSupport", false);
                newStep.NewMessage("dynamic_RequestSupport2");
                newStep.Raven = null;//Использование предметов обнуляем (меч и карты)
                newStep.BattleUser = new BattleUser
                {
                    Step = newStep.Id,
                    Game = newStep.Game,
                    BattleId = this.Id,
                    Step1 = newStep
                };
                return;
            }

            //Ходы участникам
            foreach (GameUser user in BattleUsers)
            {
                Step newStep = user.LastStep.CopyStep("Default", true);

                //Использование предметов обнуляем (меч и карты)
                newStep.Raven = null;
                newStep.BattleUser = new BattleUser
                {
                    Step = newStep.Id,
                    Game = newStep.Game,
                    BattleId = this.Id,
                    Step1 = newStep
                };

                //выбор карты дома после поддержки
                if (supportUser.Count() == 0 || (IsAttackUserNeedSupport != true && IsDefenceUserNeedSupport != true))
                    user.NewBattleUser(this.Id);
                else
                    newStep.GetStrength();
            }

            //Ходы поддержки
            if (supportUser.Count() > 0 && (IsAttackUserNeedSupport == true || IsDefenceUserNeedSupport == true))
                supportUser.ForEach(p => p.NewSupport(this.Id));
        }

        private List<RandomDesk> randomDesk;
        public void UpdateBattle()
        {
            BattleUsers.ForEach(p => p.LastStep.CopyStep("Сражение", true));

            //До боя
            if (TyrionLannister() || Dragon_Queen_of_Thorns() || AeronDamphair() || Dragon_Aeron_Damphair() || Dragon_Qyburn() || DoranMartell() || QueenofThornes())
            {
                BattleUsers.ForEach(p => p.LastStep.GetStrength());
                return;
            }

            MaceTyrell();
            Dragon_Stannis_Baratheon();
            Dragon_Walder_Frey();

            //Меч и карты перевеса
            GameUser bladeUser = BattleUsers.SingleOrDefault(p => p.LastStep.GameUserInfo.BladeInfluence == 1);
            if (bladeUser != null)
            {
                if (!Blade(bladeUser))
                    return;
            }
            else
                Random();

            SalladhorSaan();
            BattleUsers.ForEach(p => p.LastStep.GetStrength(true));
            Dragon_Margaery_Tyrell();

            //определяем победителя и проигравшего
            _BattleUsers = BattleUsers.OrderByDescending(p => p.LastStep.BattleUser.Strength.Value).ThenBy(p => p.LastStep.GameUserInfo.BladeInfluence).ToList();

            foreach (GameUser item in BattleUsers)
                item.LastStep.BattleUser.IsWinner = item == BattleUsers[0];

            #region потери и ранения
            //Карты домов
            int killedUserCount = 0;
            HomeCardType winHomeCard = BattleUsers[0].LastStep.BattleUser.LocalHomeCardType;
            if (winHomeCard != null)
                killedUserCount += winHomeCard.Attack;
            HomeCardType losHomeCard = BattleUsers[1].LastStep.BattleUser.LocalHomeCardType;
            if (losHomeCard != null)
                killedUserCount -= losHomeCard.Defence;

            //карты перевеса
            RandomDesk winRandomCard = BattleUsers[0].LastStep.BattleUser.LocalRandomCard;
            if (winRandomCard != null)
            {
                if (winRandomCard.Attack)
                    killedUserCount++;
            }
            RandomDesk losRandomCard = BattleUsers[1].LastStep.BattleUser.LocalRandomCard;
            if (losRandomCard != null)
            {
                if (losRandomCard.Defence)
                    killedUserCount--;
            }

            //специализация
            NymeriaSand(ref killedUserCount);
            SerDavosSeaworth(ref killedUserCount);
            TheonGreyjoy(ref killedUserCount);
            AshaGreyjoy(ref killedUserCount);
            TheBlackfish(ref killedUserCount);
            Dragon_Ramsay_Bolton(ref killedUserCount);
            Dragon_Doran_Martell(ref killedUserCount);

            UpdateUsedHomeCard();

            List<Unit> loserUnit = BattleUsers[1].LastStep.GameUserInfo.Unit.Where(p => p.Terrain == this.DefenceTerrain).OrderBy(p => p.UnitType1.Strength).ToList();
            foreach (Unit item in loserUnit)
            {
                if (item.IsWounded || item.UnitType == "Осадная_башня")
                {
                    BattleUsers[1].LastStep.GameUserInfo.Unit.Remove(item);
                    BattleUsers[1].LastStep.NewMessage(string.Format("dynamic_unitRemove0*unitType_{0}*unitRemoveType_{1}",
                        item.UnitType,
                        item.IsWounded ? "finished" : "thrown"));
                }
                else
                {
                    if (killedUserCount > 0)
                    {
                        BattleUsers[1].LastStep.GameUserInfo.Unit.Remove(item);
                        BattleUsers[1].LastStep.NewMessage(string.Format("dynamic_unitRemove0*unitType_{0}*unitRemoveType_destroyed",
                            item.UnitType));
                        killedUserCount--;
                    }
                    else
                    {
                        item.IsWounded = true;
                    }
                }
            }

            //Черепа
            if (winRandomCard != null && winRandomCard.Skull
                && !(losHomeCard?.Name == "Бринден_Чёрная_Рыба" && !(LoserUser.LastStep.BattleUser.AdditionalEffect?.StartsWith("Block") ?? false)))
            {
                Unit skullUnite = BattleUsers[1].LastStep.GameUserInfo.Unit.Where(p => p.Terrain == this.DefenceTerrain).OrderBy(p => p.UnitType1.Strength).FirstOrDefault();
                if (skullUnite != null)
                {
                    BattleUsers[1].LastStep.GameUserInfo.Unit.Remove(skullUnite);
                    BattleUsers[1].LastStep.NewMessage(string.Format("dynamic_unitRemove1*unitType_{0}*unitRemoveType_destroyed*text_skull",
                        skullUnite.UnitType));
                }
            }

            if (losRandomCard != null && losRandomCard.Skull
                && !(winHomeCard?.Name == "Бринден_Чёрная_Рыба" && !(WinnerUser.LastStep.BattleUser.AdditionalEffect?.StartsWith("Block") ?? false)))
            {
                Unit skullUnite = BattleUsers[0].LastStep.GameUserInfo.Unit.Where(p => p.Terrain == this.DefenceTerrain).OrderBy(p => p.UnitType1.Strength).FirstOrDefault();
                if (skullUnite != null)
                {
                    BattleUsers[0].LastStep.GameUserInfo.Unit.Remove(skullUnite);
                    BattleUsers[0].LastStep.NewMessage(string.Format("dynamic_unitRemove1*unitType_{0}*unitRemoveType_destroyed*text_skull",
                        skullUnite.UnitType));
                }
            }
            #endregion

            TywinLannister();
            Dragon_Qarl_the_Maid();
            if (!ArianneMartell())
                SerLorasTyrell();

            //Удаляем отыгравшие приказы и гарнизон
            this.LocalAttackUser.LastStep.GameUserInfo.Order.Remove(this.LocalAttackUser.LastStep.GameUserInfo.Order.SingleOrDefault(p => p.Terrain == this.AttackTerrain));
            BattleUsers[1].LastStep.GameUserInfo.Order.Remove(BattleUsers[1].LastStep.GameUserInfo.Order.SingleOrDefault(p => p.Terrain == this.DefenceTerrain));
            if (BattleUsers[1] == this.LocalDefenceUser)
            {
                if (_Game.GameInfo.Garrison.Any(p => p.Terrain1 == this.LocalDefenceTerrain))
                {
                    Step newVesterosStep = _Game.Vesteros.LastStep.CopyStep("Default", true);
                    newVesterosStep.NewMessage("dynamic_garrisonRemove*terrain_" + this.DefenceTerrain);
                    _Game.GameInfo.Garrison.Remove(_Game.GameInfo.Garrison.Single(p => p.Terrain1 == this.LocalDefenceTerrain));
                }
            }

            //Автоматическое отступление оставшихся неудачников (Робб перехватывает отступление противника)
            if (this.LocalAttackUser == this.LoserUser
                && !(WinnerUser.LastStep.BattleUser.HomeCardType == "Робб_Старк" && !(WinnerUser.LastStep.BattleUser.AdditionalEffect?.StartsWith("Block") ?? false)))
                foreach (Unit item in BattleUsers[1].LastStep.GameUserInfo.Unit.Where(p => p.Terrain == this.DefenceTerrain).OrderByDescending(p => p.UnitType1.Strength).ToList())
                    AutoRetreatUnit(BattleUsers[1], item, this.LocalAttackTerrain);

            //Смена владений
            TerrainHold(BattleUsers[0], this.LocalDefenceTerrain);
            TerrainHold(this.LocalAttackUser, this.LocalAttackTerrain);

            if (_Game.LastHomeSteps.All(p => p.IsFull))
                AfterBattle();
            else
                _Game.LastHomeSteps.Where(p => !p.IsFull && p.StepType == "Роспуск_войск").ToList().ForEach(p => p.March.SourceOrder = "AfterBattle");
        }

        //Меч
        private bool Blade(GameUser bladeUser)
        {
            //Мечь не использовали
            if (_Game.LastHomeSteps.All(p => !p.GameUserInfo.IsBladeUse) && bladeUser.LastStep.Raven == null)
            {
                BattleUsers.ForEach(p => p.LastStep.GetStrength());
                Random();

                Step newStep = bladeUser.LastStep.CopyStep("Валирийский_меч", false);
                newStep.NewRaven();
                newStep.NewMessage("dynamic_planning*stepType_Валирийский_меч");
                return false;
            }
            //Меч использовали для замены карты перевеса
            else if (bladeUser.LastStep.Raven != null && bladeUser.LastStep.Raven.StepType == "Карта_перевеса")
            {
                Random();
                bladeUser.LastStep.BattleUser.RandomDeskId = randomDesk.GetRandomCardId();
            }
            //Меч использовали раньше
            else if (bladeUser.LastStep.BattleUser.RandomDeskId == null)
                Random();

            return true;
        }

        //карты перевеса
        private void Random()
        {
            if (_Game.RandomIndex > 0 || _Game.IsRandomSkull)
            {
                IEnumerable<RandomDesk> desk = _Game.DbContext.RandomDesk.Where(p => p.Strength <= _Game.RandomIndex);
                if (!_Game.IsRandomSkull)
                    desk = desk.Where(p => p.Skull == _Game.IsRandomSkull);
                randomDesk = desk.ToList();

                BattleUsers.ForEach((p) =>
                {
                    if (!p.LastStep.BattleUser.RandomDeskId.HasValue)
                        p.LastStep.BattleUser.RandomDeskId = randomDesk.GetRandomCardId();
                    else
                        randomDesk.Remove(randomDesk.Single(p1 => p1.Id == p.LastStep.BattleUser.RandomDeskId.Value));
                });
            }
        }

        private void AutoRetreatUnit(GameUser user, Unit item, Terrain terrain)
        {
            GameUser holder = _Game.GetTerrainHolder(terrain);
            if (holder == null || holder == user)
            {
                string startTerrain = item.Terrain;

                item.Terrain = terrain.Name;
                item.Terrain1 = terrain;

                if (user.LastStep.CheckSupply(this.LocalDefenceTerrain) > 0)
                {
                    user.LastStep.GameUserInfo.Unit.Remove(item);
                    user.LastStep.NewMessage(string.Format("dynamic_unitRemove1*unitType_{0}*unitRemoveType_disbanding0*text_supplyLimit",
                        item.UnitType));
                    return;
                }

                _Game.GameHost.ArrowsData.Add(new ArrowModel() { StartTerrainName = startTerrain, EndTerrainName = terrain.Name, ArrowType = ArrowType.Retreat });

                user.LastStep.NewMessage(string.Format("dynamic_unitRemove0*unitType_{0}*unitRemoveType_retreats",
                    item.UnitType));
                return;
            }

            user.LastStep.GameUserInfo.Unit.Remove(item);
            user.LastStep.NewMessage(string.Format("dynamic_unitRemove1*unitType_{0}*unitRemoveType_disbanding0*unitRemoveType_noRetreats",
                item.UnitType));
        }

        public void AfterBattle()
        {
            if (LoserUser.LastStep.GameUserInfo.Unit.Count(p => p.Terrain1 == this.LocalDefenceTerrain) > 0)
            {
                //куда впринципе можно отступить
                List<Terrain> retreatTerrain = GetRetreatTerrain(out int minRetreatCount);

                //отступление не возможно
                if (retreatTerrain.Count == 0)
                {
                    Step newStep = LoserUser.LastStep.CopyStep("Default", true);
                    List<Unit> retreatUnit = newStep.GameUserInfo.Unit.Where(p => p.Terrain1 == this.LocalDefenceTerrain).ToList();
                    retreatUnit.ForEach(p =>
                    {
                        newStep.GameUserInfo.Unit.Remove(p);
                        newStep.NewMessage(string.Format("dynamic_unitRemove1*unitType_{0}*unitRemoveType_disbanding0*unitRemoveType_noRetreats", p.UnitType));
                    });
                    AfterBattle();
                }
                //Возможен единственный вариант
                else if (retreatTerrain.Count == 1)
                {
                    Step newStep = LoserUser.LastStep.CopyStep("Default", true);
                    List<Unit> retreatUnit = newStep.GameUserInfo.Unit.Where(p => p.Terrain1 == this.LocalDefenceTerrain).OrderByDescending(p => p.UnitType1.Strength).ToList();
                    retreatUnit.ForEach(p => AutoRetreatUnit(LoserUser, p, retreatTerrain[0]));
                    TerrainHold(LoserUser, retreatTerrain[0]);
                    AfterBattle();
                }
                //Множество вариантов
                else if (!RobbStark(WinnerUser, LoserUser))
                {
                    Step newStep = LoserUser.LastStep.CopyStep("Отступление", false);
                    newStep.NewMarch();
                    newStep.NewMessage("dynamic_planning*stepType_Отступление");
                    newStep.NewMessage("dynamic_retreatsCount*" + minRetreatCount);
                }
            }
            else
            {
                //карты после боя
                if (AfterBattleCard())
                    GameHost.NewDoStep(_Game, _Game.DbContext.DoType.Single(p => p.Sort == 1), _Game.ThroneProgress);
            }
        }

        public List<Terrain> GetRetreatTerrain(out int minRetreatCount)
        {
            Terrain defanceTerrain = this.LocalDefenceTerrain;
            List<Terrain> retreatTerrain = defanceTerrain.GetRetreatTerrain(_Game, LoserUser, defanceTerrain.TerrainType == "Земля")
                .OrderBy(p => LoserUser.LastStep.GameUserInfo.Unit.Count(p1 => p1.Terrain1 == p)).ToList();
            if (LocalAttackUser == WinnerUser)// || (WinnerUser.LastStep.BattleUser.HomeCardType == "Робб_Старк" && !(WinnerUser.LastStep.BattleUser.AdditionalEffect?.StartsWith("Block") ?? false)))
                retreatTerrain.Remove(this.LocalAttackTerrain);
            if (LoserUser.LastStep.BattleUser.HomeCardType == "Арианна_Мартелл" && !(LoserUser.LastStep.BattleUser.AdditionalEffect?.StartsWith("Block") ?? false))
                retreatTerrain.Remove(this.LocalDefenceTerrain);

            //тестируем
            List<Unit> retreatUnit = LoserUser.LastStep.GameUserInfo.Unit.Where(p => p.Terrain1 == defanceTerrain).ToList();

            List<IGrouping<int, Terrain>> resultGroupe = retreatTerrain.GroupBy(p =>
            {
                retreatUnit.ForEach(p1 =>
                {
                    p1.Terrain = p.Name;
                    p1.Terrain1 = p;
                });
                return LoserUser.LastStep.CheckSupply(this.LocalDefenceTerrain);
            }).Where(p => p.Key < retreatUnit.Count).OrderBy(p => p.Key).ToList();
            IGrouping<int, Terrain> result = resultGroupe.FirstOrDefault();
            minRetreatCount = result == null ? 0 : retreatUnit.Count - result.Key;

            //возвращаем обратно
            retreatUnit.ForEach(p1 =>
            {
                p1.Terrain = defanceTerrain.Name;
                p1.Terrain1 = defanceTerrain;
            });

            //return result;
            return result == null ? new List<Terrain>() : result.ToList();
        }

        private void TerrainHold(GameUser battleUser, Terrain terrain)
        {
            int unitCount = battleUser.LastStep.GameUserInfo.Unit.Count(p => p.Terrain1 == terrain);
            if (unitCount == 0)
                battleUser.LastStep.GameUserInfo.Order.Remove(battleUser.LastStep.GameUserInfo.Order.SingleOrDefault(p => p.Terrain1 == terrain));

            //Если войск и жетона не осталось (Ариана|Мейс|Черепа)
            if (unitCount == 0 && !battleUser.LastStep.GameUserInfo.PowerCounter.Any(p => p.Terrain1 == terrain))
            {
                //возвращаем родовую землю
                GameUser homeUserTerrain = _Game.HomeUsersSL.SingleOrDefault(p => p.HomeType1.Terrain == terrain.Name);

                //столица другого игрока
                if (homeUserTerrain != null)
                    _Game.NewTerrainHolder(terrain, homeUserTerrain);
                //не столица
                else
                    _Game.RemoveTerrainHolder(terrain);
            }
            //Захват территории
            else
                _Game.NewTerrainHolder(terrain, battleUser);
        }

        private void SalladhorSaan()
        {
            GameUser holder = BattleUsers.FirstOrDefault(p => p.LastStep.BattleUser.HomeCardType == "Салладор_Саан"
                && p.LastStep.BattleUser.AdditionalEffect == null);

            if (holder != null)
                holder.LastStep.GetStrength();
        }

        private bool RobbStark(GameUser winnerUser, GameUser loserUser)
        {
            if (winnerUser.LastStep.BattleUser.HomeCardType == "Робб_Старк"
                && winnerUser.LastStep.BattleUser.AdditionalEffect == null)
            {
                Step newStep = loserUser.LastStep.CopyStep("Робб_Старк", false);
                newStep.NewMarch();
                newStep.NewMessage("dynamic_planning*stepType_Робб_Старк");
                //newStep.BattleUser.AdditionalEffect = winnerUser.HomeType;

                return true;
            }
            else
                return false;
        }

        private bool QueenofThornes()
        {
            GameUser holder = BattleUsers.FirstOrDefault(p => p.LastStep.BattleUser.HomeCardType == "Королева_Шипов"
                && p.LastStep.BattleUser.AdditionalEffect == null);

            if (holder != null)
            {
                Step newStep = holder.LastStep.CopyStep("Королева_Шипов", false);
                newStep.NewMessage("dynamic_planning*stepType_Королева_Шипов");
                return true;
            }

            return false;
        }

        private bool DoranMartell()
        {
            GameUser holder = BattleUsers.FirstOrDefault(p => p.LastStep.BattleUser.HomeCardType == "Доран_Мартелл"
                && p.LastStep.BattleUser.AdditionalEffect == null);

            if (holder != null)
            {
                Step newStep = holder.LastStep.CopyStep("Доран_Мартелл", false);
                newStep.NewMessage("dynamic_planning*stepType_Доран_Мартелл");
                return true;
            }

            return false;
        }

        private bool AeronDamphair()
        {
            GameUser holder = BattleUsers.FirstOrDefault(p => p.LastStep.BattleUser.HomeCardType == "Эйерон_Сыровласый"
                 && p.LastStep.BattleUser.AdditionalEffect == null);
            if (holder != null)
            {
                if (holder.LastStep.GameUserInfo.Power < 2 || holder.LastStep.GameUserInfo.UsedHomeCard.Count >= 6)
                    return false;

                Step newStep = holder.LastStep.CopyStep("Эйерон_Сыровласый", false);
                newStep.NewMessage("dynamic_planning*stepType_Эйерон_Сыровласый");
                return true;
            }

            return false;
        }

        private bool TyrionLannister()
        {
            GameUser holder = BattleUsers.FirstOrDefault(p => p.LastStep.BattleUser.HomeCardType == "Тирион_Ланнистер");

            if (holder != null)
            {
                if (holder.LastStep.BattleUser.AdditionalEffect == null)
                {
                    Step newStep = holder.LastStep.CopyStep("Тирион_Ланнистер", false);
                    newStep.NewMessage("dynamic_planning*stepType_Тирион_Ланнистер");
                    return true;
                }
                else if (holder.LastStep.BattleUser.AdditionalEffect != string.Empty
                    && !(holder.LastStep.BattleUser.AdditionalEffect?.StartsWith("Block") ?? false))
                {
                    //разблокировка карты соперника
                    GameUser opponent = BattleUsers.First(p => p != holder);
                    UsedHomeCard usedHomeCard = opponent.LastStep.GameUserInfo.UsedHomeCard.SingleOrDefault(p => p.HomeCardType == holder.LastStep.BattleUser.AdditionalEffect);
                    if (usedHomeCard != null)
                        opponent.LastStep.GameUserInfo.UsedHomeCard.Remove(usedHomeCard);
                }
            }

            return false;
        }

        private void AshaGreyjoy(ref int killedUserCount)
        {
            if (BattleUsers[0].LastStep.BattleUser.HomeCardType == "Аша_Грейджой"
                && BattleUsers[0].LastStep.BattleUser.AdditionalEffect == null
                && !BattleUsers[0].LastStep.IsSupport)
            {
                killedUserCount += 2;
                BattleUsers[0].LastStep.NewMessage("dynamic_heroRage*hero_Аша_Грейджой*rageEffect_2*2");
            }
            if (BattleUsers[1].LastStep.BattleUser.HomeCardType == "Аша_Грейджой"
                && BattleUsers[1].LastStep.BattleUser.AdditionalEffect == null
                && !BattleUsers[1].LastStep.IsSupport)
            {
                killedUserCount--;
                BattleUsers[1].LastStep.NewMessage("dynamic_heroRage*hero_Аша_Грейджой*rageEffect_1*1");
            }
        }

        private void TheonGreyjoy(ref int killedUserCount)
        {
            if (BattleUsers[0] == this.LocalDefenceUser
                && BattleUsers[0].LastStep.BattleUser.HomeCardType == "Теон_Грейджой"
                && BattleUsers[0].LastStep.BattleUser.AdditionalEffect == null
                && this.LocalDefenceTerrain.Strength != 0)
            {
                killedUserCount++;
                BattleUsers[0].LastStep.NewMessage("dynamic_heroRage*hero_Теон_Грейджой*rageEffect_2*1");
            }
        }

        private void TywinLannister()
        {
            if (BattleUsers[0].LastStep.BattleUser.HomeCardType == "Тайвин_Ланнистер"
                && BattleUsers[0].LastStep.BattleUser.AdditionalEffect == null)
            {
                BattleUsers[0].LastStep.GameUserInfo.ChangePower(2);
                BattleUsers[0].LastStep.NewMessage("dynamic_heroRage*hero_Тайвин_Ланнистер*rageEffect_3");
                BattleUsers[0].LastStep.NewMessage(string.Format("dynamic_consolidatePower*hero_Тайвин_Ланнистер*{0}", BattleUsers[0].LastStep.GameUserInfo.Power));
            }
        }

        private void TheBlackfish(ref int killedUserCount)
        {
            if (BattleUsers[1].LastStep.BattleUser.HomeCardType == "Бринден_Чёрная_Рыба"
                && BattleUsers[1].LastStep.BattleUser.AdditionalEffect == null)
            {
                BattleUsers[1].LastStep.NewMessage("dynamic_heroRage*hero_Бринден_Чёрная_Рыба*rageEffect_4");
                killedUserCount = 0;
            }
        }

        private void SerDavosSeaworth(ref int killedUserCount)
        {
            if (BattleUsers[0].LastStep.BattleUser.HomeCardType == "Сер_Давос_Сиворт"
                && BattleUsers[0].LastStep.BattleUser.AdditionalEffect == null
                && BattleUsers[0].LastStep.GameUserInfo.UsedHomeCard.Any(p => p.HomeCardType == "Станис_Баратеон"))
            {
                killedUserCount++;
                BattleUsers[0].LastStep.NewMessage("dynamic_heroRage*hero_Сер_Давос_Сиворт*rageEffect_2*1");
            }
        }

        private void NymeriaSand(ref int killedUserCount)
        {
            if (BattleUsers[0].LastStep.BattleUser.HomeCardType == "Нимерия_Сэнд"
                && BattleUsers[0].LastStep.BattleUser.AdditionalEffect == null
                && BattleUsers[0] == this.LocalAttackUser)
            {
                killedUserCount++;
                BattleUsers[0].LastStep.NewMessage("dynamic_heroRage*hero_Нимерия_Сэнд*rageEffect_2*1");
            }
            if (BattleUsers[1].LastStep.BattleUser.HomeCardType == "Нимерия_Сэнд"
                && BattleUsers[1].LastStep.BattleUser.AdditionalEffect == null
                && BattleUsers[1] == this.LocalDefenceUser)
            {
                killedUserCount--;
                BattleUsers[1].LastStep.NewMessage("dynamic_heroRage*hero_Нимерия_Сэнд*rageEffect_1*1");
            }
        }

        private void SerLorasTyrell()
        {
            if (BattleUsers[0].LastStep.BattleUser.HomeCardType == "Сер_Лорас_Тирелл"
                && BattleUsers[0].LastStep.BattleUser.AdditionalEffect == null
                && BattleUsers[0] == this.LocalAttackUser
                && BattleUsers[0].LastStep.GameUserInfo.Unit.Count(p => p.Terrain1 == this.LocalDefenceTerrain) != 0)
            {
                Order attackOrder = BattleUsers[0].LastStep.GameUserInfo.Order.Single(p => p.Terrain1 == this.LocalAttackTerrain);
                attackOrder.Terrain = this.DefenceTerrain;
                attackOrder.Terrain1 = this.LocalDefenceTerrain;
                BattleUsers[0].LastStep.NewMessage("dynamic_heroRage*hero_Сер_Лорас_Тирелл*rageEffect_5");
            }
        }

        private void MaceTyrell()
        {
            GameUser maceTyrellHolder = BattleUsers.SingleOrDefault(p =>
                p.LastStep.BattleUser.HomeCardType == "Мейс_Тирелл"
                && p.LastStep.BattleUser.AdditionalEffect == null);

            if (maceTyrellHolder != null)
            {
                //ход потерпевшего
                GameUser opponent = maceTyrellHolder == this.LocalAttackUser
                    ? this.LocalDefenceUser
                    : this.LocalAttackUser;

                if (opponent.LastStep.BattleUser.HomeCardType == "Бринден_Чёрная_Рыба"
                    && opponent.LastStep.BattleUser.AdditionalEffect == null)
                    return;

                //уничтожение пешего
                if (opponent.LastStep.GameUserInfo.Unit.Any(p => p.Terrain == this.DefenceTerrain && p.UnitType == "Пеший_воин"))
                {
                    Step maceStep = maceTyrellHolder.LastStep.CopyStep("Default", true);
                    maceStep.BattleUser.AdditionalEffect = string.Empty;
                    maceStep.NewMessage("dynamic_heroRage*hero_Мейс_Тирелл*rageEffect_6");

                    Step step = opponent.LastStep.CopyStep("Default", true);
                    step.NewMessage("dynamic_unitRemove1*unitType_Пеший_воин*unitRemoveType_destroyed*hero_Мейс_Тирелл");
                    step.GameUserInfo.Unit.Remove(opponent.LastStep.GameUserInfo.Unit.OrderBy(p => p.IsWounded).First(p => p.Terrain == this.DefenceTerrain && p.UnitType == "Пеший_воин"));
                }
            }
        }

        private bool ArianneMartell()
        {
            if (BattleUsers[1].LastStep.BattleUser.AdditionalEffect != null
                || BattleUsers[1].LastStep.BattleUser.HomeCardType != "Арианна_Мартелл"
                || BattleUsers[1] != this.LocalDefenceUser)
                return false;

            BattleUsers[1].LastStep.NewMessage("dynamic_heroRage*hero_Арианна_Мартелл*rageEffect_7");
            //перемещаем
            foreach (Unit item in BattleUsers[0].LastStep.GameUserInfo.Unit.Where(p => p.Terrain == this.DefenceTerrain).OrderByDescending(p => p.UnitType1.Strength).ToList())
                AutoRetreatUnit(BattleUsers[0], item, this.LocalAttackTerrain);

            return true;
        }

        private bool RooseBolton(Step battleStep, bool isWinner)
        {
            if (!isWinner
                && battleStep.BattleUser.HomeCardType == "Русе_Болтон"
                && battleStep.BattleUser.AdditionalEffect == null)
            {
                battleStep.GameUserInfo.UsedHomeCard.Clear();
                battleStep.NewMessage("dynamic_heroRage*hero_Русе_Болтон*rageEffect_8");
                return true;
            }

            return false;
        }

        private void Dragon_Margaery_Tyrell()
        {
            Step step = this.LocalDefenceUser.LastStep;
            if (!(step.BattleUser.AdditionalEffect?.StartsWith("Block") ?? false)
                && step.BattleUser.HomeCardType == "dragon_Margaery_Tyrell"
                && (DefenceTerrain == this.LocalDefenceUser.HomeType1.Terrain
                || step.GameUserInfo.PowerCounter.Any(p => p.Terrain == DefenceTerrain)))
            {
                step.NewMessage("dynamic_heroRage*hero_dragon_Margaery_Tyrell*rageEffect_29");
                LocalAttackUser.LastStep.BattleUser.Strength = 2;
            }
        }

        private bool Dragon_Queen_of_Thorns()
        {
            GameUser holder = BattleUsers.FirstOrDefault(p => p.LastStep.BattleUser.HomeCardType == "dragon_Queen_of_Thorns"
                && p.LastStep.BattleUser.AdditionalEffect == null);

            if (holder != null)
            {
                holder.LastStep.NewMessage("dynamic_heroRage*hero_dragon_Queen_of_Thorns*rageEffect_30");
                GameUser opponent = BattleUsers.Single(p => p != holder);
                opponent.LastStep.BattleUser.AdditionalEffect = "Block";
            }

            return false;
        }

        private void Dragon_Walder_Frey()
        {
            GameUser holder = BattleUsers.FirstOrDefault(p => p.LastStep.BattleUser.HomeCardType == "dragon_Walder_Frey"
                && p.LastStep.BattleUser.AdditionalEffect == null);

            if (holder != null)
            {
                _Game.LastHomeSteps.Where(p => p.Support != null && p.Support.BattleId == this.Id)
                    .Where(p => BattleUsers.All(p1 => p1.Id != p.GameUser))
                    .ToList().ForEach(p =>
                    {
                        Step newStep = p.CopyStep("Default", true);
                        newStep.NewMessage("dynamic_heroRage*hero_dragon_Walder_Frey*rageEffect_0");
                        newStep.Support.SupportUser = holder.Id;
                    });
            }
        }

        private void Dragon_Ramsay_Bolton(ref int killedUserCount)
        {
            if (WinnerUser.LastStep.BattleUser.HomeCardType == "dragon_Ramsay_Bolton"
                   && WinnerUser.LastStep.BattleUser.AdditionalEffect == null
                   && WinnerUser.LastStep.GameUserInfo.UsedHomeCard.All(p => p.HomeCardType != "dragon_Reek"))
            {
                killedUserCount += 3;
                WinnerUser.LastStep.NewMessage("dynamic_heroRage*hero_dragon_Ramsay_Bolton*rageEffect_2*3");
            }
        }

        private void Dragon_Doran_Martell(ref int killedUserCount)
        {
            GameUser holder = BattleUsers.SingleOrDefault(p => p.LastStep.BattleUser.HomeCardType == "dragon_Doran_Martell"
                && p.LastStep.BattleUser.AdditionalEffect == null);
            if (holder != null)
            {
                if (holder == WinnerUser)
                {
                    killedUserCount += 6 - holder.LastStep.GameUserInfo.UsedHomeCard.Count();
                    holder.LastStep.NewMessage(string.Format("dynamic_heroRage*hero_dragon_Doran_Martell*rageEffect_2*{0}", killedUserCount));
                }
                else
                {
                    killedUserCount -= 6 - holder.LastStep.GameUserInfo.UsedHomeCard.Count();
                    holder.LastStep.NewMessage(string.Format("dynamic_heroRage*hero_dragon_Doran_Martell*rageEffect_1*{0}", killedUserCount));
                }
            }
        }

        private void Dragon_Reek1(Step battleStep)
        {
            if (battleStep.BattleUser.HomeCardType == "dragon_Reek"
                && battleStep.BattleUser.AdditionalEffect == null)
            {
                UsedHomeCard dragon_Ramsay_Bolton = battleStep.GameUserInfo.UsedHomeCard.SingleOrDefault(p => p.HomeCardType == "dragon_Ramsay_Bolton");
                if (dragon_Ramsay_Bolton != null)
                {
                    battleStep.GameUserInfo.UsedHomeCard.Remove(dragon_Ramsay_Bolton);
                    battleStep.NewMessage("dynamic_heroRage*hero_dragon_Reek*dynamic_homeCardReturn*hero_dragon_Ramsay_Bolton");
                }
            }
        }

        private void Dragon_Stannis_Baratheon()
        {
            GameUser holder = BattleUsers.SingleOrDefault(p => p.LastStep.BattleUser.HomeCardType == "dragon_Stannis_Baratheon"
                && p.LastStep.BattleUser.AdditionalEffect == null);
            if (holder == null)
                return;

            holder.LastStep.GetStrength();
            if (!holder.LastStep.IsSupport)
            {
                List<Order> supportOrders = _Game.LastHomeSteps.SelectMany(p => p.GameUserInfo.Order.Where(p1 => p1.OrderType1.DoType == "Подмога" && p1.Terrain1.TerrainTerrain1.Any(p2 => p2.Terrain == DefenceTerrain))).ToList();
                List<Step> steps = supportOrders.Select(p => p.GameUserInfo.Step1.CopyStep("Default", true)).ToList();
                steps.ForEach(p => supportOrders.ForEach(p1 =>
                {
                    if (p.GameUserInfo.Order.Remove(p.GameUserInfo.Order.SingleOrDefault(p2 => p2.FirstId == p1.FirstId)))
                        p.NewMessage(string.Format("dynamic_heroRage*hero_dragon_Stannis_Baratheon*rageEffect_23*orderType_{0}", p1.OrderType));
                }));
            }
        }

        private bool Dragon_Qyburn()
        {
            GameUser holder = BattleUsers.FirstOrDefault(p => p.LastStep.BattleUser.HomeCardType == "dragon_Qyburn"
                 && p.LastStep.BattleUser.AdditionalEffect == null);
            if (holder != null)
            {
                if (holder.LastStep.GameUserInfo.Power < 2 || _Game.LastHomeSteps.All(p => p.GameUserInfo.UsedHomeCard.Count == 0))
                    return false;

                Step newStep = holder.LastStep.CopyStep("dragon_Qyburn", false);
                newStep.NewMessage("dynamic_planning*stepType_dragon_Qyburn");
                return true;
            }

            return false;
        }

        private bool Dragon_Reek2()
        {
            if (LoserUser.LastStep.BattleUser.HomeCardType == "dragon_Reek"
                && LoserUser.LastStep.BattleUser.AdditionalEffect == null)
            {
                Step newStep = LoserUser.LastStep.CopyStep("dragon_Reek", false);
                newStep.NewMessage("dynamic_planning*stepType_dragon_Reek");
                return true;
            }

            return false;
        }

        private void Dragon_Qarl_the_Maid()
        {
            if (LoserUser.LastStep.BattleUser.HomeCardType == "dragon_Qarl_the_Maid"
                && LoserUser.LastStep.BattleUser.AdditionalEffect == null
                && LoserUser == LocalAttackUser)
            {
                LoserUser.LastStep.GameUserInfo.ChangePower(3);
                LoserUser.LastStep.NewMessage("dynamic_heroRage*hero_dragon_Qarl_the_Maid*rageEffect_3");
                LoserUser.LastStep.NewMessage(string.Format("dynamic_consolidatePower*hero_dragon_Qarl_the_Maid*{0}", LoserUser.LastStep.GameUserInfo.Power));
            }
        }

        private bool Dragon_Aeron_Damphair()
        {
            GameUser holder = BattleUsers.SingleOrDefault(p => p.LastStep.BattleUser.HomeCardType == "dragon_Aeron_Damphair"
                 && p.LastStep.BattleUser.AdditionalEffect == null);
            if (holder != null)
            {
                if (holder.LastStep.GameUserInfo.Power == 0)
                    return false;

                Step newStep = holder.LastStep.CopyStep("dragon_Aeron_Damphair", false);
                newStep.NewMessage("dynamic_planning*stepType_dragon_Aeron_Damphair");
                new Voting(newStep, "dragon_Aeron_Damphair");
                return true;
            }

            return false;
        }


        #region AfterBattle
        private List<Func<bool>> _AfterBattleFunc;
        public List<Func<bool>> AfterBattleFunc
        {
            get
            {
                if (_AfterBattleFunc == null)
                    _AfterBattleFunc = new List<Func<bool>>()
                    {
                        RenlyBaratheon,
                        CerseiLannister,
                        Patchface,
                        Dragon_Jon_Snow,
                        Dragon_Melisandre,
                        Dragon_Ser_Ilyn_Payne,
                        Dragon_Reek2,
                        Dragon_Ser_Gerris_Drinkwater,
                        Dragon_Rodrik_the_Reader1
                    };

                return _AfterBattleFunc;
            }
        }

        public bool AfterBattleCard()
        {
            //настройка
            switch (_Game.LastStep.StepType)
            {
                case "Ренли_Баратеон":
                    _AfterBattleFunc = AfterBattleFunc.Skip(1).ToList();
                    break;
                case "Серсея_Ланнистер":
                    _AfterBattleFunc = AfterBattleFunc.Skip(2).ToList();
                    break;
                case "Пестряк":
                    _AfterBattleFunc = AfterBattleFunc.Skip(3).ToList();
                    break;
                case "dragon_Jon_Snow":
                    _AfterBattleFunc = AfterBattleFunc.Skip(4).ToList();
                    break;
                case "dragon_Melisandre":
                    _AfterBattleFunc = AfterBattleFunc.Skip(5).ToList();
                    break;
                case "dragon_Ser_Ilyn_Payne":
                    _AfterBattleFunc = AfterBattleFunc.Skip(6).ToList();
                    break;
                case "dragon_Roose_Bolton2":
                    _AfterBattleFunc = AfterBattleFunc.Skip(7).ToList();
                    break;
                case "dragon_Ser_Gerris_Drinkwater":
                    _AfterBattleFunc = AfterBattleFunc.Skip(8).ToList();
                    break;
                case "dragon_Rodrik_the_Reader":
                    _AfterBattleFunc = AfterBattleFunc.Skip(9).ToList();
                    break;
            }

            return AfterBattleFunc.All(p => !p());
        }

        public bool RenlyBaratheon()
        {
            if (WinnerUser.LastStep.BattleUser.HomeCardType == "Ренли_Баратеон"
                && !(WinnerUser.LastStep.BattleUser.AdditionalEffect?.StartsWith("Block") ?? false))
            {
                if (string.IsNullOrEmpty(WinnerUser.LastStep.BattleUser.AdditionalEffect))
                    return false;

                UnitType knight = _Game.DbContext.UnitType.First(p => p.Name == "Рыцарь");
                if (WinnerUser.LastStep.GameUserInfo.Unit.Count(p => p.UnitType == "Рыцарь") >= knight.Count)
                    return false;

                Step newStep = WinnerUser.LastStep.CopyStep("Ренли_Баратеон", false);
                newStep.NewMessage("dynamic_heroRage*hero_Ренли_Баратеон*rageEffect_17");
                return true;
            }

            return false;
        }

        public bool CerseiLannister()
        {
            if (WinnerUser.LastStep.BattleUser.HomeCardType == "Серсея_Ланнистер"
                && WinnerUser.LastStep.BattleUser.AdditionalEffect == null)
            {
                Step newStep = WinnerUser.LastStep.CopyStep("Серсея_Ланнистер", false);
                newStep.NewMessage("dynamic_heroRage*hero_Серсея_Ланнистер*rageEffect_16");
                return true;
            }

            return false;
        }

        public bool Patchface()
        {
            GameUser holder = BattleUsers.SingleOrDefault(p => p.LastStep.BattleUser.HomeCardType == "Пестряк"
                && p.LastStep.BattleUser.AdditionalEffect == null);
            if (holder != null)
            {
                Step newStep = holder.LastStep.CopyStep("Пестряк", false);
                newStep.NewMessage("dynamic_heroRage*hero_Пестряк*rageEffect_15");
                return true;
            }

            return false;
        }

        public bool Dragon_Jon_Snow()
        {
            if (WinnerUser.LastStep.BattleUser.HomeCardType != "dragon_Jon_Snow"
                || WinnerUser.LastStep.BattleUser.AdditionalEffect != null)
                return false;

            Step newStep = WinnerUser.LastStep.CopyStep("dragon_Jon_Snow", false);
            newStep.NewMessage("dynamic_heroRage*hero_dragon_Jon_Snow*rageEffect_21");
            return true;
        }

        public bool Dragon_Melisandre()
        {
            GameUser holder = BattleUsers.SingleOrDefault(p => p.LastStep.BattleUser.HomeCardType == "dragon_Melisandre"
                && p.LastStep.BattleUser.AdditionalEffect == null);
            if (holder == null)
                return false;

            Step newStep = holder.LastStep.CopyStep("dragon_Melisandre", false);
            newStep.NewMessage(string.Format("dynamic_heroRage*hero_dragon_Melisandre*dynamic_homeCardReturn*?"));
            return true;
        }

        public bool Dragon_Ser_Ilyn_Payne()
        {
            if (WinnerUser.LastStep.BattleUser.HomeCardType == "dragon_Ser_Ilyn_Payne"
                && WinnerUser.LastStep.BattleUser.AdditionalEffect == null)
            {
                if (LoserUser.LastStep.GameUserInfo.Unit.Count(p => p.UnitType == "Пеший_воин") == 0)
                    return false;

                Step newStep = WinnerUser.LastStep.CopyStep("dragon_Ser_Ilyn_Payne", false);
                newStep.NewMessage("dynamic_heroRage*hero_dragon_Ser_Ilyn_Payne*rageEffect_6");
                return true;
            }

            return false;
        }

        public bool Dragon_Ser_Gerris_Drinkwater()
        {
            if (WinnerUser.LastStep.BattleUser.HomeCardType == "dragon_Ser_Gerris_Drinkwater"
                && WinnerUser.LastStep.BattleUser.AdditionalEffect == null)
            {
                Step newStep = WinnerUser.LastStep.CopyStep("dragon_Ser_Gerris_Drinkwater", false);
                newStep.NewMessage("dynamic_planning*stepType_dragon_Ser_Gerris_Drinkwater");
                return true;
            }

            return false;
        }

        public bool Dragon_Rodrik_the_Reader1()
        {
            if (WinnerUser.LastStep.BattleUser.HomeCardType == "dragon_Rodrik_the_Reader"
                && WinnerUser.LastStep.BattleUser.AdditionalEffect == null)
            {
                Step newStep = WinnerUser.LastStep.CopyStep("dragon_Rodrik_the_Reader", false);
                newStep.NewMessage("dynamic_planning*stepType_dragon_Rodrik_the_Reader1");
                return true;
            }

            return false;
        }
        #endregion

        private void UpdateUsedHomeCard()
        {
            foreach (GameUser item in BattleUsers)
            {
                if (!string.IsNullOrEmpty(item.LastStep.BattleUser.HomeCardType))
                {
                    if (item.LastStep.GameUserInfo.UsedHomeCard.Count >= 6)
                        item.LastStep.GameUserInfo.UsedHomeCard.Clear();

                    if (RooseBolton(item.LastStep, item == BattleUsers[0] ? true : false))
                        continue;

                    Dragon_Reek1(item.LastStep);
                    //if (item.LastStep.BattleUser.LocalHomeCardType.HomeType1 == item.HomeType1
                    //    && item.LastStep.GameUserInfo.UsedHomeCard.All(p => p.HomeCardType1 != item.LastStep.BattleUser.LocalHomeCardType))
                    //    new UsedHomeCard(item.LastStep.GameUserInfo, item.LastStep.BattleUser.LocalHomeCardType);
                    //else
                    //{

                    //}
                    if (!(item.LastStep.BattleUser.AdditionalEffect?.EndsWith("dragon_Qyburn") ?? false))
                        new UsedHomeCard(item.LastStep.GameUserInfo, item.LastStep.BattleUser.LocalHomeCardType);
                    else
                        new UsedHomeCard(item.LastStep.GameUserInfo, item.HomeType1.HomeCardType.Single(p => p.Name == "dragon_Qyburn"));
                }
            }
        }
    }
}
