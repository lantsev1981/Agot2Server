using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameService
{
    public partial class GameHost
    {
        static public Random Rnd = new Random();

        //все ходы исполненны
        static private void NextStage(Game game)
        {
            Step lastStep = game.HomeSteps.Last(p => p.StepType != "Default");
            GameUser user = lastStep.GameUser1;
            switch (lastStep.StepType)
            {
                case "Замысел":
                    {
                        ClearEmptyOrder(game);

                        GameUser ravenGameUser = game.HomeUsersSL.Single(p => p.LastStep.GameUserInfo.RavenInfluence == 1);
                        Step newStep = ravenGameUser.LastStep.CopyStep("Посыльный_ворон", false);
                        newStep.NewRaven();
                        newStep.NewMessage("dynamic_planning*stepType_Посыльный_ворон");
                        break;
                    }

                case "Посыльный_ворон":
                case "Неожиданный_шаг":
                case "Разведка_за_стеной":
                    {
                        game.Vesteros.LastStep.IsNew = false;
                        Step step = game.Vesteros.LastStep.CopyStep("Набег", true);
                        step.NewMessage("dynamic_planning*stepType_Замысел");

                        game.HomeSteps.ForEach(p => p.IsNew = false);
                        GameHost.NewDoStep(game, game.DbContext.DoType.Single(p => p.Sort == 0), 0);
                        break;
                    }

                case "Набег":
                    GameHost.NewDoStep(game, game.DbContext.DoType.Single(p => p.Sort == 0), game.ThroneProgress);
                    break;

                case "dragon_Rodrik_the_Reader":
                case "dragon_Ser_Gerris_Drinkwater":
                case "dragon_Reek":
                case "dragon_Ser_Ilyn_Payne":
                case "dragon_Melisandre":
                case "dragon_Jon_Snow":
                case "Робб_Старк":
                case "Ренли_Баратеон":
                case "Серсея_Ланнистер":
                case "Пестряк":
                case "Отступление":
                case "Поход":
                    GameHost.NewDoStep(game, game.DbContext.DoType.Single(p => p.Sort == 1), game.ThroneProgress);
                    break;

                case "Усиление_власти_Вестерос":
                    if (lastStep.GameUserInfo.Order.Count != 0)
                    {
                        Step newStep = lastStep.CopyStep("Усиление_власти_Вестерос", false);
                        newStep.NewMessage("dynamic_planning*stepType_Усиление_власти_Вестерос");
                    }
                    else
                        game.NewSbor();
                    break;

                case "Усиление_власти":
                    GameHost.NewDoStep(game, game.DbContext.DoType.Single(p => p.Sort == 2), game.ThroneProgress);
                    break;

                case "Роспуск_войск":
                    switch (lastStep.March.SourceOrder)
                    {
                        //Захват порта с автороспуском войск (предел количества типов)
                        case "Захват_порта":
                            GameHost.NewDoStep(game, game.DbContext.DoType.Single(p => p.Sort == 1), game.ThroneProgress);
                            break;
                    }
                    break;

                case "Подмога":
                    UpdateSupport(game);
                    break;

                case "Сражение":
                case "Тирион_Ланнистер":
                case "Эйерон_Сыровласый":
                case "dragon_Qyburn":
                case "Доран_Мартелл":
                case "Королева_Шипов":
                case "Валирийский_меч":
                case "dragon_Aeron_Damphair":
                    game.GameInfo.Battle.UpdateBattle();
                    break;

                case "Борьба_за_влияние":
                    if (UpdateVoting(game, lastStep.Voting.Target))
                        game.NextVoting(lastStep.Voting.Target);
                    break;
            }
        }

        static public bool UpdateVoting(Game game, string votingTarget)
        {
            var throneUser = game.HomeUsersSL.Single(p => p.LastStep.GameUserInfo.ThroneInfluence == 1);

            var votingResult = game.VotedUsers.OrderByDescending(p => p.LastStep.Voting.PowerCount)
                .ThenBy(p => p.LastStep.GameUserInfo.ThroneInfluence)
                .ToList();

            var gropeResult = votingResult.GroupBy(p => p.LastStep.Voting.PowerCount)
                .ToList();

            //Изменяем количество доступной власти
            foreach (var item in votingResult)
            {
                Step newStep = item.LastStep.CopyStep("Default", true);
                newStep.GameUserInfo.ChangePower(-newStep.Voting.PowerCount);
                newStep.NewMessage(string.Format("dynamic_batsCount*{0}*{1}",
                    newStep.Voting.PowerCount,
                    newStep.GameUserInfo.Power));
                if (gropeResult.Single(p => p.Key == newStep.Voting.PowerCount).Count() == 1)
                    newStep.NewMessage(string.Format("dynamic_voting*voting_{0}*{1}", votingTarget, votingResult.IndexOf(item) + 1));
            }

            if (votingTarget == "Одичалые")
            {
                BarbarianTrack(game, throneUser, votingResult, gropeResult);
                return false;
            }
            else
                return InfluenceTrack(throneUser, votingTarget, votingResult, gropeResult);
        }

        static private bool InfluenceTrack(GameUser throneUser, string votingTarget, List<GameUser> votingResult, List<IGrouping<int, GameUser>> gropeResult)
        {
            for (int i = 1; i <= throneUser.Game1.HomeUsersSL.Count; i++)
            {
                GameUser user = votingResult.ElementAt(i - 1);
                switch (votingTarget)
                {
                    case "Железный_трон": user.LastStep.GameUserInfo.ThroneInfluence = i; break;
                    case "Вотчины": user.LastStep.GameUserInfo.BladeInfluence = i; break;
                    case "Королевский_двор": user.LastStep.GameUserInfo.RavenInfluence = i; break;
                }
            }

            return CheckResolution(votingTarget, throneUser, gropeResult);
        }

        static private void BarbarianTrack(Game game, GameUser throneUser, List<GameUser> votingResult, List<IGrouping<int, GameUser>> gropeResult)
        {
            int sumPower = votingResult.Sum(p => p.LastStep.Voting.PowerCount);
            Step newVesterosStep = game.Vesteros.LastStep.CopyStep("Борьба_за_влияние", true);//Default

            if (sumPower < game.GameInfo.Barbarian)
            {
                newVesterosStep.NewMessage("dynamic_wildVictory");
                gropeResult.RemoveAll(p => p != gropeResult.Last());
                if (CheckResolution("Одичалые", throneUser, gropeResult))
                    game.NewBarbarian(false, gropeResult.Single().Single());
            }
            else
            {
                newVesterosStep.NewMessage("dynamic_wildFail");
                gropeResult.RemoveAll(p => p != gropeResult.First());
                if (CheckResolution("Одичалые", throneUser, gropeResult))
                    game.NewBarbarian(true, gropeResult.Single().Single());
            }
        }

        static private bool CheckResolution(string votingTarget, GameUser throneUser, List<IGrouping<int, GameUser>> gropeResult)
        {
            //оставляем только спорные группы
            gropeResult = gropeResult.Where(p => p.Count() > 1).ToList();

            if (gropeResult.Count > 0)
            {
                Step newStep = throneUser.LastStep.CopyStep("Железный_трон", false);
                newStep.NewMessage("dynamic_kingClaim");
                StringBuilder sb = new StringBuilder();
                gropeResult.SelectMany(p => p).ToList().ForEach(p => sb.Append(p.HomeType).Append("|"));

                //todo костыль (добавляет карту одичалых в конец списка виновников)
                if (votingTarget == "Одичалые")
                {
                    VesterosDecks vesterosDecks = throneUser.Game1.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == 4).First(p => !p.IsFull);
                    sb.Append(vesterosDecks.VesterosCardType1.Id.ToString());
                }

                newStep.NewRaven();
                newStep.Raven.StepType = sb.ToString();
                return false;
            }

            return true;
        }

        static private void UpdateSupport(Game game)
        {
            Battle battle = game.GameInfo.Battle;
            battle.BattleUsers.ForEach(p => p.NewBattleUser(battle.Id));
        }

        static private void ClearEmptyOrder(Game game)
        {
            foreach (var user in game.HomeUsersSL)
            {
                Step newStep = user.LastStep.CopyStep("Default", true);

                foreach (var order in newStep.GameUserInfo.Order.Where(p => string.IsNullOrEmpty(p.OrderType)).ToList())
                {
                    newStep.NewMessage("dynamic_orderCancel*terrain_" + order.Terrain);
                    newStep.GameUserInfo.Order.Remove(order);
                }
            }
        }

        static private void ApplyConsolidateOrder(Game game)
        {
            foreach (var user in game.HomeUsersSL.ToList())
            {
                Step step = user.LastStep;
                IEnumerable<Order> consolidateOrder = step.GameUserInfo.Order.Where(p => p.OrderType.Contains("Усиление_власти"));
                consolidateOrder = consolidateOrder.Where(p => p.OrderType != "Усиление_власти_0_специальный" || p.Terrain1.Strength == 0).ToList();

                if (consolidateOrder.Count() > 0)
                {
                    Step newStep = step.CopyStep("Усиление_власти", true);

                    consolidateOrder = newStep.GameUserInfo.Order.Where(p => p.OrderType.Contains("Усиление_власти"));
                    consolidateOrder = consolidateOrder.Where(p => p.OrderType != "Усиление_власти_0_специальный" || p.Terrain1.Strength == 0).ToList();
                    foreach (var item in consolidateOrder)
                        item.СollectConsolidate(user);
                }
            }
        }

        static public void NewDoStep(Game game, DoType lastDoType, int throneProgress)
        {
            if (throneProgress < 0 || throneProgress >= game.HomeUsersSL.Count) throneProgress = 0;
            GameUser nextUser = game.HomeUsersSL[throneProgress++];

            if (nextUser.LastStep.GameUserInfo.Order.Any(p1 => p1.OrderType.Contains(lastDoType.Name)))
            {
                //Новый ход с нового листа
                nextUser.LastStep.IsNew = false;
                Step newDoStep = nextUser.LastStep.CopyStep(lastDoType.Name, false);
                if (newDoStep.StepType != "Набег") newDoStep.NewMarch();
                newDoStep.NewMessage("dynamic_planning*stepType_" + newDoStep.StepType);
                game.ThroneProgress = throneProgress;
            }
            else
            {
                if (!game.LastHomeSteps.Any(p => p.GameUserInfo.Order.Any(p1 => p1.OrderType.Contains(lastDoType.Name))))
                {
                    throneProgress = 0;
                    //Не требующие внимания игрока сбор власти
                    if (lastDoType.Sort == 1) ApplyConsolidateOrder(game);

                    //запускаем обработку следующего типа приказов;
                    DoType nextDoType = game.DbContext.DoType.SingleOrDefault(p => p.Sort == lastDoType.Sort + 1);

                    if (nextDoType != null) NewDoStep(game, nextDoType, throneProgress);
                    else
                    {
                        //конец игры
                        if (game.GameInfo.Turn == 10) game.TheEnd();
                        else game.NewTurn();
                    }
                }
                else NewDoStep(game, lastDoType, throneProgress);
            }
        }
    }
}
