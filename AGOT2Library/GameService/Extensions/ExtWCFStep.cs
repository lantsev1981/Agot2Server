using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;



namespace GameService
{
    internal static class ExtWCFStep
    {
        internal static void Update(this WCFStep o, Step serverStep)
        {
            //serverStep.IsFull = true;

            GameUser user = serverStep.GameUser1;
            Game game = user.Game1;
            Step newStep = serverStep.CopyStep(serverStep.StepType, true);

            switch (o.StepType)
            {

                case "Железный_трон":
                    //расстановка короля
                    newStep.Raven = o.Raven.ToRaven(newStep);
                    string[] result = newStep.Raven.StepType.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                    string lastVotingTarget;
                    //TODO был косяк если король не учавствовал в битве одичалых
                    if (game.LastHomeSteps.Any(p => p.Voting != null && p.Voting.Target == "Одичалые"))
                    {
                        lastVotingTarget = "Одичалые";
                        newStep.Raven.StepType = result[0];
                    }
                    else
                    {
                        lastVotingTarget = serverStep.Voting.Target;
                        //если не все дома то король забил
                        if (result.Length == game.HomeUsersSL.Count)
                        {
                            for (int i = 0; i < result.Length; i++)
                            {
                                //пропускаем неспорные дома
                                if (!serverStep.Raven.StepType.Contains(result[i]))
                                    continue;

                                int pos = i + 1;
                                GameUser votingUser = user.Game1.HomeUsersSL.Single(p => p.HomeType == result[i]);
                                Step resultStep = votingUser.LastStep.CopyStep("Железный_трон", true);
                                resultStep.NewMessage(string.Format("dynamic_voting*voting_{0}*{1}", serverStep.Voting.Target, pos));

                                switch (serverStep.Voting.Target)
                                {
                                    case "Железный_трон": votingUser.LastStep.GameUserInfo.ThroneInfluence = pos; break;
                                    case "Вотчины": votingUser.LastStep.GameUserInfo.BladeInfluence = pos; break;
                                    case "Королевский_двор": votingUser.LastStep.GameUserInfo.RavenInfluence = pos; break;
                                }
                            }
                        }
                    }

                    user.Game1.NextVoting(lastVotingTarget);
                    break;

                case "Замысел":
                case "Неожиданный_шаг":
                    foreach (Order item in newStep.GameUserInfo.Order)
                    {
                        WCFOrder wcfOrder = o.GameUserInfo.Order.Single(p => p.Id == item.FirstId);
                        item.OrderType = wcfOrder.OrderType;
                        item.OrderType1 = string.IsNullOrEmpty(wcfOrder.OrderType) ? null : newStep.GameUser1.Game1.DbContext.OrderType.Single(p => p.Name == wcfOrder.OrderType);
                    }
                    break;

                case "Валирийский_меч":
                    newStep.Raven = o.Raven.ToRaven(newStep);
                    switch (newStep.Raven.StepType)
                    {
                        case "Валирийский_меч":
                            newStep.GameUserInfo.IsBladeUse = true;
                            newStep.NewMessage("dynamic_bladePower*rageEffect_9");
                            break;

                        case "Карта_перевеса":
                            newStep.GameUserInfo.IsBladeUse = true;
                            newStep.NewMessage("dynamic_bladePower*rageEffect_19");
                            break;

                        case null:
                            newStep.NewMessage("dynamic_bladePower*rageEffect_18");
                            break;
                    }
                    break;

                case "Посыльный_ворон":
                    newStep.Raven = o.Raven.ToRaven(newStep);
                    switch (newStep.Raven.StepType)
                    {
                        case "Неожиданный_шаг":
                            {
                                Step newThikStep = newStep.CopyStep("Неожиданный_шаг", false);
                                newThikStep.NewMessage("dynamic_planning*stepType_Неожиданный_шаг");
                                break;
                            }
                        case "Разведка_за_стеной":
                            {
                                if (game.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == 4).All(p => p.IsFull))
                                    game.ResetVesterosDesc(4);

                                VesterosDecks vesterosDecks = game.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == 4).First(p => !p.IsFull);

                                //TODO Защитить разведку от других игроков
                                Step newThikStep = newStep.CopyStep("Разведка_за_стеной", false);
                                newThikStep.NewMessage("dynamic_planning*stepType_Разведка_за_стеной");
                                newThikStep.Raven.StepType = vesterosDecks.VesterosCardType1.Id.ToString();
                                break;
                            }
                    }
                    break;

                case "Разведка_за_стеной":
                    if (o.Raven.StepType == "Yes")
                    {
                        Step newVesterosStep = game.Vesteros.LastStep.CopyStep("Разведка_за_стеной", true);
                        newVesterosStep.NewMessage("dynamic_wildBuries");

                        List<VesterosDecks> vesterosDecksList = game.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == 4).ToList();
                        VesterosDecks vesterosDecks = vesterosDecksList.First(p => !p.IsFull);

                        vesterosDecks.Sort = vesterosDecksList.Last().Sort + 1;
                    }
                    break;

                case "Набег":
                    o.UpdateRaid(newStep);
                    break;

                case "Поход":
                    o.UpdateMarch(newStep);
                    break;

                case "Робб_Старк":
                case "Отступление":
                    o.UpdateRetreat(newStep);
                    break;

                case "Роспуск_войск":
                    newStep.March = o.March.ToMarch(newStep);
                    UpdateUnit(newStep);

                    if (game.LastHomeSteps.All(p => p.IsFull))
                    {
                        switch (newStep.March.SourceOrder)
                        {
                            //Изменение снабжения
                            case "Снабжение_войск":
                                game.NextVesterosCard();
                                break;

                            //Захват пустого порта
                            case "Захват_порта":
                                GameHost.NewDoStep(game, game.DbContext.DoType.Single(p => p.Sort == 1), game.ThroneProgress);
                                break;

                            //создаётся до битвы
                            case "Захват_порта_до_сражения":
                                game.GameInfo.Battle.Start();
                                break;

                            //Захват порта после битвы
                            case "AfterBattle":
                                game.GameInfo.Battle.AfterBattle();
                                break;
                        }
                    }
                    break;

                case "Усиление_власти_Вестерос":
                case "Усиление_власти":
                    o.UpdateConsolidate(newStep);
                    break;

                case "Подмога":
                    o.UpdateSupport(newStep);
                    break;

                case "RequestSupport":
                    {
                        Step newVesterosStep = game.Vesteros.LastStep.CopyStep("Сражение", true);
                        game.GameInfo.Battle.IsDefenceUserNeedSupport = o.IsNeedSupport ?? false;
                        game.GameInfo.Battle.Start();
                        break;
                    }

                case "Сражение":
                    if (game.GameUser.Where(p => p.IsCapitulated).SelectMany(p => p.HomeType1.HomeCardType).Any(p => p.Name == o.BattleUser.HomeCardType))
                        throw new AntiCheatException(user.Id, "SendStep.Update.CheckUsedHomeCard.IsCapitulated");
                    if (game.LastHomeSteps.SelectMany(p => p.GameUserInfo.UsedHomeCard).Any(p => p.HomeCardType == o.BattleUser.HomeCardType)
                        && !(newStep.BattleUser.AdditionalEffect?.EndsWith("dragon_Qyburn") ?? false))
                        throw new AntiCheatException(user.Id, "SendStep.Update.CheckUsedHomeCard");

                    newStep.BattleUser.HomeCardType = o.BattleUser.HomeCardType;
                    break;

                case "Тирион_Ланнистер":
                    o.TyrionLannister(newStep);
                    break;

                case "dragon_Qyburn":
                    o.ChangeCard(newStep, true);
                    break;

                case "Эйерон_Сыровласый":
                    o.ChangeCard(newStep, false);
                    break;

                case "Доран_Мартелл":
                    o.DoranMartell(newStep);
                    break;

                case "Королева_Шипов":
                    o.OrderCancel(newStep);
                    break;

                case "Серсея_Ланнистер":
                    o.OrderCancel(newStep);
                    game.GameInfo.Battle.AfterBattleCard();
                    break;

                #region Ренли_Баратеон
                case "Ренли_Баратеон":
                    if (newStep.BattleUser.AdditionalEffect == o.BattleUser.AdditionalEffect)
                        newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_18", newStep.BattleUser.HomeCardType));
                    else
                    {
                        if (!newStep.BattleUser.AdditionalEffect.Contains(o.BattleUser.AdditionalEffect))
                            throw new AntiCheatException(user.Id, "dynamic_Renly_Baratheon");

                        newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_17", newStep.BattleUser.HomeCardType));
                        Unit unit = newStep.GameUserInfo.Unit.First(p => p.FirstId == Guid.Parse(o.BattleUser.AdditionalEffect));
                        unit.UnitType = "Рыцарь";
                        unit.UnitType1 = newStep.GameUser1.Game1.DbContext.UnitType.Single(p => p.Name == "Рыцарь");

                        newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
                    }

                    game.GameInfo.Battle.AfterBattleCard();//new
                    break;
                #endregion

                case "dragon_Ser_Ilyn_Payne":
                    if (newStep.BattleUser.AdditionalEffect == o.BattleUser.AdditionalEffect)
                        newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_18", newStep.BattleUser.HomeCardType));
                    else
                    {
                        Guid unitId = Guid.Parse(o.BattleUser.AdditionalEffect);
                        if (!game.GameInfo.Battle.LoserUser.LastStep.GameUserInfo.Unit.Any(p => p.FirstId == unitId && p.UnitType == "Пеший_воин"))
                            throw new AntiCheatException(user.Id, "Нарушение павил (Violation of rules)");

                        Step step = game.GameInfo.Battle.LoserUser.LastStep.CopyStep("dragon_Ser_Ilyn_Payne", true);
                        Unit unit = game.GameInfo.Battle.LoserUser.LastStep.GameUserInfo.Unit.FirstOrDefault(p => p.FirstId == Guid.Parse(o.BattleUser.AdditionalEffect));
                        step.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_6", newStep.BattleUser.HomeCardType));
                        RemoveUnit(step, unit);

                        newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
                    }

                    game.GameInfo.Battle.AfterBattleCard();//new
                    break;

                case "Пестряк":
                    o.Patchface(newStep);
                    game.GameInfo.Battle.AfterBattleCard();//new
                    break;

                case "dragon_Jon_Snow":
                    {
                        Step newVesterosStep = null;
                        switch (o.BattleUser.AdditionalEffect)
                        {
                            case "Up":
                                if (game.GameInfo.Barbarian < 10)
                                {
                                    newVesterosStep = game.Vesteros.LastStep.CopyStep("dragon_Jon_Snow", true);
                                    newVesterosStep.GameInfo.Barbarian += 2;
                                }
                                break;
                            case "Down":
                                if (game.GameInfo.Barbarian > 0)
                                {
                                    newVesterosStep = game.Vesteros.LastStep.CopyStep("dragon_Jon_Snow", true);
                                    newVesterosStep.GameInfo.Barbarian -= 2;
                                }
                                break;
                        }
                        if (newVesterosStep == null)
                            newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_18", newStep.BattleUser.HomeCardType));
                        else
                            newVesterosStep.NewMessage("dynamic_barbarianThreat*" + newVesterosStep.GameInfo.Barbarian);

                        newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;


                        game.GameInfo.Battle.AfterBattleCard();//new
                    }
                    break;

                case "dragon_Reek":
                    {
                        if (!string.IsNullOrEmpty(o.BattleUser.AdditionalEffect))
                        {
                            newStep.GameUserInfo.UsedHomeCard.Remove(newStep.GameUserInfo.UsedHomeCard.Single(p => p.HomeCardType == "dragon_Reek"));
                            newStep.NewMessage("dynamic_heroRage*hero_dragon_Reek*dynamic_homeCardReturn*hero_dragon_Reek");
                        }

                        newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
                        game.GameInfo.Battle.AfterBattleCard();
                        break;
                    }

                case "dragon_Rodrik_the_Reader":
                    {
                        if (!string.IsNullOrEmpty(o.BattleUser.AdditionalEffect))
                        {
                            int.TryParse(o.BattleUser.AdditionalEffect, out int descNum);
                            if (descNum != 0)
                            {
                                newStep = newStep.CopyStep("dragon_Rodrik_the_Reader", false);
                                List<string> cards = game.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == descNum && !p.IsFull).Select(p => p.VesterosCardType).ToList();
                                newStep.BattleUser.AdditionalEffect = JsonConvert.SerializeObject(cards);
                            }
                            else
                            {
                                newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
                                VesterosDecks card = game.VesterosDecks.First(p => p.VesterosCardType1.Id == newStep.BattleUser.AdditionalEffect);
                                game.ResetVesterosDesc(card.VesterosCardType1.DecksNumber);
                                card = game.VesterosDecks.First(p => p.VesterosCardType1.Id == newStep.BattleUser.AdditionalEffect);
                                card.Sort = game.VesterosDecks.First(p => p.VesterosCardType1.DecksNumber == card.VesterosCardType1.DecksNumber).Sort - 1;
                                game.GameInfo.Battle.AfterBattleCard();
                            }
                        }
                        else
                            game.GameInfo.Battle.AfterBattleCard();

                        break;
                    }

                case "Борьба_за_влияние":
                    o.Voting.CopyTo(newStep);
                    break;

                #region Событие_Вестероса
                case "Событие_Вестероса":
                    {
                        newStep.VesterosAction = o.VesterosAction.ToVesterosAction(newStep);

                        Step newVesterosStep = game.Vesteros.LastStep.CopyStep("Событие_Вестероса", true);//Default
                        VesterosDecks vesterosDecks = game.VesterosDecks.Single(p => p.FirstId == newStep.VesterosAction.VesterosDecks);
                        VesterosCardAction vesterosCardAction = vesterosDecks.VesterosCardType1.VesterosCardAction.Single(p => p.ActionNumber == newStep.VesterosAction.ActionNumber.Value);
                        vesterosDecks.VesterosActionType = vesterosCardAction.VesterosActionType;
                        vesterosDecks.VesterosActionType1 = vesterosCardAction.VesterosActionType1;
                        newVesterosStep.NewMessage("dynamic_vesterosEvent*actionType_" + vesterosCardAction.VesterosActionType);

                        game.VesterosAction(vesterosDecks.VesterosActionType);
                    }
                    break;
                #endregion

                #region Сбор_на_Молоководной
                case "Сбор_на_Молоководной":
                    {
                        HomeCardType card = game.DbContext.HomeCardType.Single(p => p.Name == o.Raven.StepType);
                        new UsedHomeCard(newStep.GameUserInfo, card);
                        newStep.NewMessage("dynamic_homeCardDiscards*hero_" + card.Name);
                        if (game.LastHomeSteps.All(p => p.IsFull))
                            game.NextVesterosCard();
                        break;
                    }
                #endregion

                #region Наездники_на_мамонтах
                case "Наездники_на_мамонтах":
                    {
                        if (!string.IsNullOrEmpty(o.Raven.StepType))
                        {
                            HomeCardType card = game.DbContext.HomeCardType.Single(p => p.Name == o.Raven.StepType);
                            newStep.GameUserInfo.UsedHomeCard.Remove(newStep.GameUserInfo.UsedHomeCard.Single(p => p.HomeCardType1 == card));
                            newStep.NewMessage("dynamic_homeCardReturn*hero_" + card.Name);
                        }
                        game.NextVesterosCard();
                        break;
                    }
                #endregion

                #region Наездники_на_мамонтах_роспуск_войск
                case "Наездники_на_мамонтах_роспуск_войск":
                    {
                        int unitCount = int.Parse(newStep.Raven.StepType);
                        if (o.March.MarchUnit.Count(p => p.UnitType == null) != unitCount)
                            throw new AntiCheatException(user.Id, string.Format("Наездники на мамонтах -> Необходимо уничтожить {0} любых отряда (Riders on mammoths -> destroy {0} any unit)", unitCount));

                        newStep.March = o.March.ToMarch(newStep);
                        UpdateUnit(newStep);

                        if (game.LastHomeSteps.All(p => p.IsFull))
                            game.NextVesterosCard();
                        break;
                    }
                #endregion

                #region "Король-за-Стеной"
                case "Король-за-Стеной":
                    {
                        Symbolic symbolic = game.DbContext.Symbolic.SingleOrDefault(p => p.Name == o.Raven.StepType);
                        if (symbolic == null || symbolic.Name == "Карта_одичалых")
                            new AntiCheatException(user.Id, "Король-за-Стеной -> выберите жетон превосходства одного из треков влияния (A king-beyond-the-Wall -> select token of superiority of one of the tracks of influence)");

                        if (newStep.Raven.StepType == ChangeTrackEffect.First.ToString())
                        {
                            //TODO повтор изменения треков влияния
                            List<GameUser> homeUser = null;
                            switch (symbolic.Name)
                            {
                                case "Железный_трон":
                                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.ThroneInfluence < newStep.GameUserInfo.ThroneInfluence).ToList();
                                    break;
                                case "Валирийский_меч":
                                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.BladeInfluence < newStep.GameUserInfo.BladeInfluence).ToList();
                                    break;
                                case "Посыльный_ворон":
                                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.RavenInfluence < newStep.GameUserInfo.RavenInfluence).ToList();
                                    break;
                            }

                            homeUser.ForEach(p => p.ChangeTrack(symbolic.Name, ChangeTrackEffect.Down));
                            user.ChangeTrack(symbolic.Name, ChangeTrackEffect.First);
                        }
                        else
                        {
                            if (symbolic.Name == "Железный_трон")
                                new AntiCheatException(user.Id, "Король-за-Стеной -> выберите жетон превосходства трека Вотчины или Двора (A king-beyond-the-Wall -> select token of the superiority of the Fiefdoms track or King's Court)");

                            //TODO повтор изменения треков влияния
                            List<GameUser> homeUser = null;
                            switch (symbolic.Name)
                            {
                                case "Валирийский_меч":
                                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.BladeInfluence > newStep.GameUserInfo.BladeInfluence).ToList();
                                    break;
                                case "Посыльный_ворон":
                                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.RavenInfluence > newStep.GameUserInfo.RavenInfluence).ToList();
                                    break;
                            }

                            homeUser.ForEach(p => p.ChangeTrack(symbolic.Name, ChangeTrackEffect.Up));
                            user.ChangeTrack(symbolic.Name, ChangeTrackEffect.Last);

                            GameUser nextUser = game.VotedUsers.First(p => p.LastStep.GameUserInfo.ThroneInfluence > newStep.GameUserInfo.ThroneInfluence);
                            if (nextUser.LastStep.GameUserInfo.ThroneInfluence != game.HomeUsersSL.Count)
                            {
                                newStep = nextUser.LastStep.CopyStep("Король-за-Стеной", false);
                                newStep.NewMessage("dynamic_wildlingsResult*event_a_king_beyond_the_wall*dynamic_wildlingsCategory0");
                                newStep.NewRaven();
                                newStep.Raven.StepType = ChangeTrackEffect.Last.ToString();
                            }
                        }

                        if (game.LastHomeSteps.All(p => p.IsFull))
                            game.NextVesterosCard();
                    }
                    break;
                #endregion

                case "dragon_Ser_Gerris_Drinkwater":
                    {
                        Symbolic symbolic = game.DbContext.Symbolic.SingleOrDefault(p => p.Name == o.BattleUser.AdditionalEffect);
                        if (symbolic == null)
                            return;

                        if (symbolic.Name == "Карта_одичалых")
                            new AntiCheatException(user.Id, "dragon_Ser_Gerris_Drinkwater -> выберите жетон превосходства одного из треков влияния (dragon_Ser_Gerris_Drinkwater -> select token of superiority of one of the tracks of influence)");

                        //TODO повтор изменения треков влияния
                        newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
                        GameUser homeUser = null;
                        switch (newStep.BattleUser.AdditionalEffect)
                        {
                            case "Железный_трон":
                                homeUser = game.HomeUsersSL.SingleOrDefault(p => p.LastStep.GameUserInfo.ThroneInfluence == newStep.GameUserInfo.ThroneInfluence - 1);
                                break;
                            case "Валирийский_меч":
                                homeUser = game.HomeUsersSL.SingleOrDefault(p => p.LastStep.GameUserInfo.BladeInfluence == newStep.GameUserInfo.BladeInfluence - 1);
                                break;
                            case "Посыльный_ворон":
                                homeUser = game.HomeUsersSL.SingleOrDefault(p => p.LastStep.GameUserInfo.RavenInfluence == newStep.GameUserInfo.RavenInfluence - 1);
                                break;
                        }

                        if (homeUser == null)
                            return;

                        homeUser.ChangeTrack(newStep.BattleUser.AdditionalEffect, ChangeTrackEffect.Down);
                        newStep.GameUser1.ChangeTrack(newStep.BattleUser.AdditionalEffect, ChangeTrackEffect.Up);

                    }
                    break;

                #region Передовой_отряд
                case "Передовой_отряд":
                    {
                        int destroyCount = o.March.MarchUnit.Count(p => p.UnitType == null);
                        if (destroyCount == 0)
                        {
                            Symbolic symbolic = game.DbContext.Symbolic.SingleOrDefault(p => p.Name == o.Raven.StepType);
                            if (symbolic == null || !newStep.Raven.StepType.Contains(symbolic.Name))
                                throw new AntiCheatException(user.Id, "Передовой_отряд -> выберите одно из двух: НИЗШАЯ СТАВКА теряет 2 любых отряда, либо отступает на 2 деления по тому треку влияния, где у неё наилучшая позиция (Preemptive Raid -> choose one: LOWER the BET loses 2 of any unit, or retreats on 2 division at the track effects, where her best position)");

                            //TODO повтор изменения треков влияния
                            List<GameUser> homeUser = null;
                            switch (symbolic.Name)
                            {
                                case "Железный_трон":
                                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.ThroneInfluence - newStep.GameUserInfo.ThroneInfluence <= 2
                                        && p.LastStep.GameUserInfo.ThroneInfluence - newStep.GameUserInfo.ThroneInfluence > 0).ToList();
                                    break;
                                case "Валирийский_меч":
                                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.BladeInfluence - newStep.GameUserInfo.BladeInfluence <= 2
                                         && p.LastStep.GameUserInfo.BladeInfluence - newStep.GameUserInfo.BladeInfluence > 0).ToList();
                                    break;
                                case "Посыльный_ворон":
                                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.RavenInfluence - newStep.GameUserInfo.RavenInfluence <= 2
                                         && p.LastStep.GameUserInfo.RavenInfluence - newStep.GameUserInfo.RavenInfluence > 0).ToList();
                                    break;
                            }

                            homeUser.ForEach(p => p.ChangeTrack(symbolic.Name, ChangeTrackEffect.Up));
                            user.ChangeTrack(symbolic.Name, ChangeTrackEffect.Down);
                            user.ChangeTrack(symbolic.Name, ChangeTrackEffect.Down);
                        }
                        else
                        {
                            if (destroyCount > 2)
                                throw new AntiCheatException(user.Id, "Передовой_отряд -> Вы не можете уничтожить более 2х отрядов (Preemptive Raid -> You can't kill more than 2 units)");
                            if (o.March.MarchUnit.Count >= 2 && destroyCount < 2)
                                throw new AntiCheatException(user.Id, "Передовой_отряд -> Вы должны уничтожить 2 отряда (Preemptive Raid ->You have to destroy 2 unit)");

                            newStep.March = o.March.ToMarch(newStep);
                            UpdateUnit(newStep);
                        }

                        game.NextVesterosCard();
                        break;
                    }
                #endregion

                #region Наступление_орды_Усиление_власти
                case "Наступление_орды_Усиление_власти":
                    {
                        newStep.March = o.March.ToMarch(newStep);
                        UpdateUnit(newStep);
                        game.NextVesterosCard();
                        break;
                    }
                #endregion

                #region Наступление_орды
                case "Наступление_орды":
                    {
                        int needDestroyCount = int.Parse(newStep.Raven.StepType);
                        int destroyCount = o.March.MarchUnit.Count(p => p.UnitType == null);
                        if (destroyCount > needDestroyCount || (newStep.GameUserInfo.Unit.Count >= needDestroyCount && destroyCount < needDestroyCount))
                            throw new AntiCheatException(user.Id, string.Format("Наступление_орды -> Вы должны уничтожить количество отрядов = {0} (The Horde Descends -> You have to destroy a number of units = {0})", needDestroyCount));

                        newStep.March = o.March.ToMarch(newStep);

                        if (needDestroyCount == 2 && newStep.GameUserInfo.GameUserTerrain.Select(p => p.Terrain1).Any(p => p.Strength > 0 && newStep.GameUserInfo.Unit.Count(p1 => p1.Terrain1 == p) >= 2))
                        {
                            try
                            {
                                Terrain unitTerrain = newStep.March.MarchUnit.Where(p => p.UnitType == null).Select(p => p.LocalTerrain).Distinct().Single();
                                if (unitTerrain.Strength == 0)
                                    throw new Exception();
                            }
                            catch { throw new AntiCheatException(user.Id, "Наступление_орды -> Вы должны уничтожить 2 отряда в одном из своих замков или крепостей (The Horde Descends -> You have to destroy 2 units in one of the castles or fortresses)"); }
                        }

                        UpdateUnit(newStep);

                        if (game.LastHomeSteps.All(p => p.IsFull))
                            game.NextVesterosCard();
                        break;
                    }
                #endregion

                #region Убийцы_ворон
                case "Убийцы_ворон":
                    {
                        if (o.March.MarchUnit.Count(p => p.UnitType == null) > 2)
                            throw new AntiCheatException(user.Id, "Убийцы_ворон -> Максимальное количество изменяемых отрядов = 2 (Crow Killers -> Maksimalno the number of variable units = 2)");

                        if (newStep.Raven.StepType == "Upgrade")
                            o.March.MarchUnit.Where(p => p.UnitType == null).ToList().ForEach(p => p.UnitType = "Рыцарь");
                        else
                        {
                            if (o.March.MarchUnit.Count(p => p.UnitType == null) < 2)
                                throw new AntiCheatException(user.Id, "Убийцы_ворон -> Минимальное количество изменяемых отрядов = 2 (Crow Killers -> The minimum number of variable units = 2)");
                            o.March.MarchUnit.Where(p => p.UnitType == null).ToList().ForEach(p => p.UnitType = "Пеший_воин");
                        }

                        newStep.March = o.March.ToMarch(newStep);
                        UpdateUnit(newStep);

                        if (game.LastHomeSteps.All(p => p.IsFull))
                            game.NextVesterosCard();
                        break;
                    }
                #endregion

                #region dragon_Melisandre
                case "dragon_Melisandre":
                    {
                        if (!string.IsNullOrEmpty(o.BattleUser.AdditionalEffect))
                        {
                            HomeCardType card = game.DbContext.HomeCardType.Single(p => p.Name == o.BattleUser.AdditionalEffect);
                            if (card.Strength > newStep.GameUserInfo.Power)
                                throw new AntiCheatException(user.Id, "dragon_Melisandre -> card.Strength > newStep.GameUserInfo.Power");

                            newStep.GameUserInfo.UsedHomeCard.Remove(newStep.GameUserInfo.UsedHomeCard.Single(p => p.HomeCardType1 == card));
                            newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
                            newStep.GameUserInfo.ChangePower(-card.Strength);
                            newStep.NewMessage(string.Format("dynamic_consolidatePowerLoses*hero_{0}*{1}", newStep.BattleUser.HomeCardType, newStep.GameUserInfo.Power));
                            newStep.NewMessage("dynamic_heroRage*hero_dragon_Melisandre*dynamic_homeCardReturn*hero_" + card.Name);
                        }

                        game.GameInfo.Battle.AfterBattleCard();
                        break;
                    }
                #endregion

                case "dragon_Aeron_Damphair":
                    {
                        newStep.BattleUser.AdditionalEffect = o.Voting.PowerCount.ToString();
                        newStep.GameUserInfo.ChangePower(-o.Voting.PowerCount);
                        newStep.NewMessage(string.Format("dynamic_consolidatePowerLoses*hero_{0}*{1}", newStep.BattleUser.HomeCardType, newStep.GameUserInfo.Power));
                        break;
                    }
            }
        }

        public static void UpdateUnit(Step newStep)
        {
            GameUser user = newStep.GameUser1;
            Game game = user.Game1;

            //Сначала создание юнитов потому что они могут быть уничтожены при обработке следующих действий
            foreach (MarchUnit item in newStep.March.MarchUnit.ToList())
            {
                Unit unit = newStep.GameUserInfo.Unit.SingleOrDefault(p => p.FirstId == item.Unit);
                if (unit == null)
                {
                    //Создание юнита
                    unit = new Unit(newStep.GameUserInfo, item.LocalUnitType, item.LocalTerrain);
                    game.NewTerrainHolder(unit.Terrain1, user);

                    //неучаствует в последующей обработке
                    newStep.March.MarchUnit.Remove(item);
                }
            }

            foreach (MarchUnit item in newStep.March.MarchUnit.ToList())
            {
                Unit unit = newStep.GameUserInfo.Unit.SingleOrDefault(p => p.FirstId == item.Unit);
                if (unit == null)
                    continue;

                if (item.UnitType == null)
                    RemoveUnit(newStep, unit);
                else
                {
                    //редактирование юнита
                    UnitType type = item.LocalUnitType;
                    if (unit.UnitType1 != type)
                    {
                        if (newStep.GameUserInfo.Unit.Count(p => p.UnitType1 == type) < type.Count)
                        {
                            unit.UnitType = type.Name;
                            unit.UnitType1 = type;
                        }
                        else
                        {
                            //распускаем если downgrade
                            if (unit.UnitType1.Strength > type.Strength)
                                RemoveUnit(newStep, unit);
                        }
                    }

                    Terrain terrain = item.LocalTerrain;
                    if (unit.Terrain1 != terrain)
                    {
                        game.GameHost.ArrowsData.Add(new ArrowModel() { StartTerrainName = unit.Terrain, EndTerrainName = terrain.Name, ArrowType = ArrowType.Retreat });

                        unit.Terrain = terrain.Name;
                        unit.Terrain1 = terrain;
                        game.NewTerrainHolder(terrain, user);
                    }
                }
            }
        }

        private static void RemoveUnit(Step newStep, Unit unit)
        {
            GameUser user = newStep.GameUser1;
            Game game = user.Game1;

            //Уничтожение юнита
            newStep.GameUserInfo.Unit.Remove(unit);
            newStep.NewMessage(string.Format("dynamic_unitRemove0*unitType_{0}*{1}",
                unit.UnitType,
                unit.UnitType != "Корабль" ? "unitRemoveType_disbanding0" : "unitRemoveType_disbanding1"));

            //если войск не осталось убираем приказ
            if (!newStep.GameUserInfo.Unit.Any(p => p.Terrain1 == unit.Terrain1))
                newStep.GameUserInfo.Order.Remove(newStep.GameUserInfo.Order.SingleOrDefault(p => p.Terrain == unit.Terrain));

            //возвращаем территорию если не закрепили власть
            if (game.GetTerrainHolder(unit.Terrain1) == user//территория игрока
                && user.HomeType1.Terrain != unit.Terrain//не столица игрока
                && !newStep.GameUserInfo.Unit.Any(p => p.Terrain1 == unit.Terrain1)//нет войск
                && !newStep.GameUserInfo.PowerCounter.Any(p => p.Terrain1 == unit.Terrain1))//нет жетона
            {
                GameUser homeUserTerrain = game.HomeUsersSL.SingleOrDefault(p => p.HomeType1.Terrain == unit.Terrain);
                //не столица
                if (homeUserTerrain == null)
                    game.RemoveTerrainHolder(unit.Terrain1);
                //столица другого игрока
                else
                {
                    //Если ход игрока влияет на ход другого, то по очереди
                    //TODO подумать над лучшим решением
                    Step lastStep = homeUserTerrain.LastStep;
                    if (!lastStep.IsFull && !lastStep.IsNew)
                        throw new AntiCheatException(user.Id, string.Format("Необходимо дождаться завершения хода Дома \"{0}\" (You must wait for the completion step of the another House)", homeUserTerrain.HomeType));

                    Step returnStep = homeUserTerrain.LastStep.CopyStep("Default", true);
                    returnStep.NewMessage(string.Format("dynamic_powerEstablishes*terrain_" + unit.Terrain));
                    game.NewTerrainHolder(unit.Terrain1, homeUserTerrain);
                }
            }
        }

        private static void OrderCancel(this WCFStep o, Step newStep)
        {
            GameUser user = newStep.GameUser1;
            Game game = user.Game1;
            newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
            if (string.IsNullOrEmpty(newStep.BattleUser.AdditionalEffect))
            {
                newStep.BattleUser.AdditionalEffect = string.Empty;
                newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_18", newStep.BattleUser.HomeCardType));
            }
            else
            {
                GameUser opponent = game.GameInfo.Battle.LocalAttackUser == user
                    ? game.GameInfo.Battle.LocalDefenceUser
                    : game.GameInfo.Battle.LocalAttackUser;
                Step opponentStep = opponent.LastStep.CopyStep(newStep.StepType, true);
                Order order = opponentStep.GameUserInfo.Order.First(p => p.FirstId == Guid.Parse(o.BattleUser.AdditionalEffect));
                opponentStep.GameUserInfo.Order.Remove(order);
                opponentStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_23*orderType_{1}", newStep.BattleUser.HomeCardType, order.OrderType));
            }
        }

        private static void Patchface(this WCFStep o, Step newStep)
        {
            GameUser user = newStep.GameUser1;
            Game game = user.Game1;
            newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
            if (string.IsNullOrEmpty(newStep.BattleUser.AdditionalEffect))
            {
                newStep.BattleUser.AdditionalEffect = string.Empty;
                newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_18", newStep.BattleUser.HomeCardType));
            }
            else
            {
                GameUser opponent = game.GameInfo.Battle.LocalAttackUser == user
                    ? game.GameInfo.Battle.LocalDefenceUser
                    : game.GameInfo.Battle.LocalAttackUser;
                Step opponentStep = opponent.LastStep.CopyStep("Default", true);
                opponentStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_24*hero_{1}", newStep.BattleUser.HomeCardType, o.BattleUser.AdditionalEffect));

                if (opponentStep.GameUserInfo.UsedHomeCard.Count == 6)
                    opponentStep.GameUserInfo.UsedHomeCard.Clear();
                HomeCardType card = game.DbContext.HomeCardType.Single(p => p.Name == o.BattleUser.AdditionalEffect);
                new UsedHomeCard(opponentStep.GameUserInfo, card);
            }
        }

        private static void DoranMartell(this WCFStep o, Step newStep)
        {
            GameUser user = newStep.GameUser1;
            Game game = user.Game1;
            newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
            GameUser opponent = game.GameInfo.Battle.LocalAttackUser == user
                ? game.GameInfo.Battle.LocalDefenceUser
                : game.GameInfo.Battle.LocalAttackUser;

            //TODO повтор изменения треков влияния
            List<GameUser> homeUser = null;
            switch (newStep.BattleUser.AdditionalEffect)
            {
                case "Железный_трон":
                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.ThroneInfluence > opponent.LastStep.GameUserInfo.ThroneInfluence).ToList();
                    break;
                case "Валирийский_меч":
                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.BladeInfluence > opponent.LastStep.GameUserInfo.BladeInfluence).ToList();
                    break;
                case "Посыльный_ворон":
                    homeUser = game.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.RavenInfluence > opponent.LastStep.GameUserInfo.RavenInfluence).ToList();
                    break;
            }

            homeUser.ForEach(p => p.ChangeTrack(newStep.BattleUser.AdditionalEffect, ChangeTrackEffect.Up));
            opponent.ChangeTrack(newStep.BattleUser.AdditionalEffect, ChangeTrackEffect.Last);
        }

        internal static void ChangeCard(this WCFStep o, Step newStep, bool isDragon_Qyburn)
        {
            newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
            if (string.IsNullOrEmpty(o.BattleUser.AdditionalEffect))
            {
                newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_18", newStep.BattleUser.HomeCardType));
            }
            else
            {
                newStep.GameUserInfo.ChangePower(-2);
                newStep.NewMessage(string.Format("dynamic_consolidatePowerLoses*hero_{0}*{1}", newStep.BattleUser.HomeCardType, newStep.GameUserInfo.Power));
                newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_25", newStep.BattleUser.HomeCardType));

                new UsedHomeCard(newStep.GameUserInfo, newStep.BattleUser.LocalHomeCardType);

                if (isDragon_Qyburn)
                    newStep.BattleUser.AdditionalEffect = "Block|dragon_Qyburn";
                else
                    newStep.BattleUser.AdditionalEffect = null;

                newStep = newStep.CopyStep("Сражение", false);
                newStep.BattleUser.HomeCardType = null;
                newStep.NewMessage("dynamic_homeCardSelect");
                newStep.GetStrength();
            }
        }

        private static void TyrionLannister(this WCFStep o, Step newStep)
        {
            GameUser user = newStep.GameUser1;
            Game game = user.Game1;
            newStep.BattleUser.AdditionalEffect = o.BattleUser.AdditionalEffect;
            if (!string.IsNullOrEmpty(o.BattleUser.AdditionalEffect))
                newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_22*hero_{1}", newStep.BattleUser.HomeCardType, newStep.BattleUser.AdditionalEffect));
            else
            {
                newStep.NewMessage(string.Format("dynamic_heroRage*hero_{0}*rageEffect_18", newStep.BattleUser.HomeCardType));
                return;
            }

            Battle battle = game.GameInfo.Battle;
            GameUser opponent = battle.LocalAttackUser == newStep.GameUser1
                ? battle.LocalDefenceUser
                : battle.LocalAttackUser;

            Step newBattleStep = opponent.LastStep.CopyStep("Сражение", true);
            new UsedHomeCard(newBattleStep.GameUserInfo, newBattleStep.BattleUser.LocalHomeCardType);
            //newBattleStep.IsFull = newBattleStep.GameUserInfo.UsedHomeCard.Count == 7 ? true : false;
            newBattleStep = newBattleStep.CopyStep("Сражение", newBattleStep.GameUserInfo.UsedHomeCard.Count == 7 ? true : false);

            newBattleStep.BattleUser.HomeCardType = null;
            newBattleStep.NewMessage(newBattleStep.IsFull
                ? string.Format("dynamic_heroRage*hero_{0}*rageEffect_20", newStep.BattleUser.HomeCardType)
                : "dynamic_homeCardSelect");

            newStep.GetStrength();
            newBattleStep.GetStrength();
        }

        private static void UpdateRetreat(this WCFStep o, Step newStep)
        {
            GameUser user = newStep.GameUser1;
            Game game = user.Game1;

            List<IGrouping<string, WCFMarchUnit>> unitGroup = o.March.MarchUnit.GroupBy(p => p.Terrain).ToList();
            if (unitGroup.Count == 0)
                throw new AntiCheatException(user.Id, "Отступление -> вы обязаны отступить с наименьшими потерями (Deviation -> you are obliged to retreat with minimal losses)!");
            if (unitGroup.Count > 1)
                throw new AntiCheatException(user.Id, "Отступление -> вы не можете отступать в разных направлениях (Deviation -> you can't retreat in different directions)!");

            //Поиск территорий возможных для отступления
            List<Terrain> retreatTerrain = game.GameInfo.Battle.GetRetreatTerrain(out int minRetreatCount);
            if (unitGroup[0].Count() < minRetreatCount)
                throw new AntiCheatException(user.Id, "Отступление -> вы обязаны отступить с наименьшими потерями (Deviation -> you are obliged to retreat with minimal losses)!");

            Terrain terrain = retreatTerrain.SingleOrDefault(p => p.Name == unitGroup[0].Key);
            if (terrain == null)
                throw new AntiCheatException(user.Id, "Отступление -> вы не можете отступать на территорию: с которой началась атака, с превышением снабжения или в чужие земли (Deviation -> you can't retreat to the territory: which began the attack, with excess supply or in foreign lands)!");

            //TODO Защита от подмены данных
            unitGroup[0].ToList().ForEach(p => p.UnitType = newStep.GameUserInfo.Unit.Single(p1 => p1.FirstId == p.Unit).UnitType);
            newStep.March = o.March.ToMarch(newStep);
            UpdateUnit(newStep);

            //ищем неотступивших юнитов и удаляем их
            newStep.GameUserInfo.Unit.Where(p => p.Terrain == game.GameInfo.Battle.LocalDefenceTerrain.Name).ToList().
                ForEach(p => newStep.GameUserInfo.Unit.Remove(p));

            game.GameInfo.Battle.AfterBattleCard();
        }



        private static void UpdateSupport(this WCFStep o, Step newStep)
        {
            newStep.Support = o.Support.ToSupport(newStep);
            GameUser supportUser = newStep.Support.SupportUser == null
                ? null
                : newStep.GameUser1.Game1.HomeUsersSL.Single(p => p.Id == newStep.Support.SupportUser);

            if (supportUser == null)
            {
                newStep.Support = null;
            }
            else
            {
                GameUser user = newStep.GameUser1;
                Game game = user.Game1;
                Battle battle = game.GameInfo.Battle;
                ArrowType arrowType = battle.DefenceUser == supportUser.Id ? ArrowType.Support : ArrowType.Attack;
                List<string> supportTerrain = newStep.GameUserInfo.Order
                    .Where(p => battle.LocalDefenceTerrain.TerrainType == "Море" ? p.Terrain1.TerrainType != "Земля" : p.Terrain1.TerrainType != "Порт")
                    .Where(p => p.OrderType1.DoType == "Подмога" && p.Terrain1.TerrainTerrain1.Any(p1 => p1.Terrain == battle.DefenceTerrain)).Select(p => p.Terrain).Distinct().ToList();
                supportTerrain.ForEach(p => game.GameHost.ArrowsData.Add(new ArrowModel() { StartTerrainName = p, EndTerrainName = battle.DefenceTerrain, ArrowType = arrowType }));

                newStep.NewMessage("dynamic_homeSupport*homeType_" + supportUser.HomeType);
            }
        }

        private static void UpdateConsolidate(this WCFStep o, Step newStep)
        {
            GameUser user = newStep.GameUser1;
            Order sourceOrder = newStep.GameUserInfo.Order.Single(p => p.FirstId == Guid.Parse(o.March.SourceOrder));
            newStep.March = o.March.ToMarch(newStep);
            //если нет новых или изменённых юнитов добавляем влияния
            if (newStep.March.MarchUnit.Count == 0 && newStep.StepType != "Усиление_власти_Вестерос")
                sourceOrder.СollectConsolidate(user);
            else
            {
                //удаление приказа
                newStep.GameUserInfo.Order.Remove(sourceOrder);
                UpdateUnit(newStep);

                if (newStep.CheckSupply() > 0)
                    throw new AntiCheatException(user.Id, "UpdateConsolidate.CheckSupply");
            }
        }

        private static void UpdateMarch(this WCFStep o, Step newStep)
        {
            GameUser user = newStep.GameUser1;
            Game game = user.Game1;

            game.GameHost.ArrowsData.Clear();

            Order sourceOrder = newStep.GameUserInfo.Order.First(p => p.FirstId == Guid.Parse(o.March.SourceOrder));

            Terrain defenceTerrain = null;
            foreach (string terrainName in o.March.MarchUnit.Select(p => p.Terrain).Distinct().ToList())
            {
                //ищем владельца
                Terrain terrain = game.DbContext.Terrain.Single(p => p.Name == terrainName);
                GameUser holder = game.GetTerrainHolder(terrain);
                //обнаружены вражеские войска или вражеский гарнизон
                bool newTerrain = true;
                if (holder != null && holder != user
                    && (holder.LastStep.GameUserInfo.Unit.Any(p => p.Terrain1 == terrain)
                        || game.GameInfo.Garrison.SingleOrDefault(p => p.Terrain1 == terrain) != null))
                {
                    newTerrain = false;
                    //первая битва
                    if (defenceTerrain == null)
                        defenceTerrain = terrain;
                    //вторая битва игнорится
                    else
                        continue;
                }

                //перемещаем юнитов
                foreach (WCFMarchUnit item in o.March.MarchUnit.Where(p => p.Terrain == terrain.Name).ToList())
                {
                    Unit moveUnit = newStep.GameUserInfo.Unit.Single(p => p.FirstId == item.Unit);

                    game.GameHost.ArrowsData.Add(new ArrowModel() { StartTerrainName = moveUnit.Terrain, EndTerrainName = item.Terrain, ArrowType = defenceTerrain != terrain ? ArrowType.March : ArrowType.Attack });

                    moveUnit.Terrain = item.Terrain;
                    moveUnit.Terrain1 = terrain;
                }

                LandTransfer(o, newStep, user, game, sourceOrder);

                if (newTerrain)
                    game.NewTerrainHolder(terrain, user);
            }

            if (newStep.CheckSupply() > 0)
                throw new AntiCheatException(user.Id, "UpdateMarch.CheckSupply");

            //при отсутсвие битв удаляем приказ похода
            if (defenceTerrain == null)
                user.LastStep.GameUserInfo.Order.Remove(newStep.GameUserInfo.Order.Single(p => p.Terrain == sourceOrder.Terrain));
            else
                game.NewBattle(user, sourceOrder.Terrain1, defenceTerrain, o.IsNeedSupport ?? true);
        }

        private static void LandTransfer(WCFStep o, Step newStep, GameUser user, Game game, Order sourceOrder)
        {
            //Игнорим если столица игрока
            if (user.HomeType1.Terrain == sourceOrder.Terrain)
                return;
            //Проверяем право владения землёй из которой совершён поход
            if (newStep.GameUserInfo.Unit.Count(p => p.Terrain1 == sourceOrder.Terrain1) == 0
                && !newStep.GameUserInfo.PowerCounter.Any(p => p.Terrain1 == sourceOrder.Terrain1))
            {
                //укрепляем власть
                if (o.March.IsTerrainHold)
                {
                    newStep.GameUserInfo.NewPowerCounter(sourceOrder.Terrain1);
                    newStep.GameUserInfo.Power--;
                }
                //освобождаем территорию
                else
                {
                    GameUser homeUserTerrain = game.HomeUsersSL.SingleOrDefault(p => p.HomeType1.Terrain == sourceOrder.Terrain);

                    //не столица
                    if (homeUserTerrain == null)
                        game.RemoveTerrainHolder(sourceOrder.Terrain1);
                    //столица другого игрока
                    else
                    {
                        Step returnStep = homeUserTerrain.LastStep.CopyStep("Default", true);
                        returnStep.NewMessage("dynamic_powerEstablishes*terrain_" + sourceOrder.Terrain);
                        game.NewTerrainHolder(sourceOrder.Terrain1, homeUserTerrain);
                    }
                }
            }
        }

        //Обнавляет набег
        private static void UpdateRaid(this WCFStep o, Step newStep)
        {
            GameUser user = newStep.GameUser1;

            Order sourceOrder = newStep.GameUserInfo.Order.Single(p => p.FirstId == o.Raid.SourceOrder);
            newStep.GameUserInfo.Order.Remove(sourceOrder);
            newStep.Raid = o.Raid.ToRaid(newStep);

            if (o.Raid.TargetOrder != null)
            {
                Order targetOrder = user.Game1.LastHomeSteps.SelectMany(p => p.GameUserInfo.Order).Single(p => p.FirstId == o.Raid.TargetOrder);
                newStep.NewMessage("dynamic_raided*terrain_" + targetOrder.Terrain);

                Step targetStep = targetOrder.GameUserInfo.Step1;
                if (targetOrder.OrderType.Contains("Усиление_власти"))
                {
                    targetOrder.СollectConsolidate(user);
                }
                else
                {
                    Step newTargetStep = targetStep.CopyStep("Default", true);
                    newTargetStep.GameUserInfo.Order.Remove(newTargetStep.GameUserInfo.Order.Single(p => p.Terrain == targetOrder.Terrain));
                }
            }
            else
                newStep.NewMessage("dynamic_orderCancel*terrain_" + sourceOrder.Terrain);
        }
    }
}
