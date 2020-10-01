using ChatServer;
using GamePortal;
using MyLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameService;

namespace GameService
{
    public partial class Game
    {
        public GameHost GameHost { get; set; }
        public Agot2p6Entities DbContext { get; set; }
        public GameUser CurrentUser { get; set; }

        private GameUser _Vesteros;
        /// <summary>
        /// Специальный пользователь содержащий в себе общую информацию об игре
        /// </summary>
        public GameUser Vesteros
        {
            get
            {
                if (_Vesteros == null)
                    _Vesteros = this.GameUser.Single(p => p.Login == "Вестерос");
                return _Vesteros;
            }
        }

        /// <summary>
        /// Последняя общая информация об игре
        /// </summary>
        public GameInfo GameInfo => this.Vesteros.LastStep.GameInfo;

        /// <summary>
        /// Отсортированный список карточных колод вестероса и одичалых
        /// </summary>
        public List<VesterosDecks> VesterosDecks => this.GameInfo.VesterosDecks.OrderBy(p => p.Sort).ToList();

        private Step _LastStep;
        /// <summary>
        /// Последний ход в игре
        /// </summary>
        public Step LastStep
        {
            get => _LastStep ?? LastHomeSteps.Last();
            set => _LastStep = value;
        }

        /// <summary>
        /// Отсортированный список последних ходов пользователей
        /// </summary>
        public List<Step> LastHomeSteps => this.HomeUsersSL.Select(p => p.LastStep).OrderBy(p => p.Id).ToList();

        /// <summary>
        /// Отсортированный список ходов пользователей
        /// </summary>
        public List<Step> HomeSteps => this.HomeUsersSL.SelectMany(p => p.Step).OrderBy(p => p.Id).ToList();

        /// <summary>
        /// Отсортированный список всех ходов 
        /// </summary>
        public List<Step> AllSteps => this.GameUser.SelectMany(p => p.Step).OrderBy(p => p.Id).ToList();

        /// <summary>
        /// Отсортированный по трону список пользователей
        /// </summary>
        public List<GameUser> HomeUsersSL => this.GameUser.Where(p => !string.IsNullOrEmpty(p.HomeType) && !p.IsCapitulated)
                    .OrderBy(p => p.LastStep.GameUserInfo.ThroneInfluence).ToList();

        public List<GameUser> VotedUsers => HomeUsersSL.Where(p => p.LastStep.Voting != null).ToList();

        public WCFRateSettings RateSettings => new WCFRateSettings()
        {
            MindRate = this.MindRate,
            HonorRate = this.HonorRate,
            LikeRate = this.LikeRate,
            DurationRate = this.DurationRate,
        };

        // GameService.Game.ToWCFGame	113 511	63	21,79%	0,01%
        public WCFGame ToWCFGame()
        {
            WCFGame result = new WCFGame
            {
                Id = this.Id,
                CreateTime = this.CreateTime,
                OpenTime = this.OpenTime,
                CloseTime = this.CloseTime,

                Settings = new WCFGameSettings()
                {
                    GameType = this.Type,
                    CreatorLogin = this.CreatorLogin,
                    Name = this.Name,
                    HasPassword = !string.IsNullOrEmpty(this.Password),
                    RandomIndex = this.RandomIndex,
                    IsRandomSkull = this.IsRandomSkull,
                    RateSettings = this.RateSettings,
                    MaxTime = this.MaxTime,
                    AddTime = this.AddTime,
                    Lang = this.Lang,
                    WithoutChange = this.WithoutChange,
                    IsGarrisonUp = this.IsGarrisonUp,
                    NoTimer = this.NoTimer
                },

                GameUser = new List<WCFGameUser>()
            };
            foreach (GameUser item in this.GameUser.ToList())
                result.GameUser.Add(item.ToWCFGameUser());

            return result;
        }

        public Game CopyGame()
        {
            Game result = new Game
            {
                DbContext = this.DbContext,

                Id = Guid.NewGuid(),
                CreateTime = DateTimeOffset.UtcNow
            };

            this.Vesteros.CopyGameUser(result);
            foreach (GameUser item in this.HomeUsersSL)
                item.CopyGameUser(result);

            //создание карточных колод
            List<VesterosDecks> vesterosDecksList = new List<VesterosDecks>();
            foreach (VesterosCardType item in result.DbContext.VesterosCardType.ToList())
            {
                for (int i = 0; i < item.Count; i++)
                    vesterosDecksList.Add(new VesterosDecks(item));
            }

            foreach (VesterosDecks item in vesterosDecksList)
                item.CopyVesterosDecks(result.GameInfo);

            result.ResetVesterosDesc(1);
            result.ResetVesterosDesc(2);
            result.ResetVesterosDesc(3);
            result.ResetVesterosDesc(4);

            //#if DEBUG
            //            result.AddAllContent();
            //#endif

            return result;
        }

        #region Для расстановки всех элементов на карте
        private void AddAllContent()
        {

            Step firstStep = LastHomeSteps.First();
            Step lastStep = LastHomeSteps.Last();

            DbContext.Terrain.ToList().ForEach(p =>
            {
                Order order = new Order
                {
                    Id = Guid.NewGuid()
                };
                order.FirstId = order.Id;
                order.Step = firstStep.Id;
                order.Game = this.Id;
                order.Terrain = p.Name;
                firstStep.GameUserInfo.Order.Add(order);

                if (p.TerrainType == "Земля")
                {
                    PowerCounter powerCounter = new PowerCounter()
                    {
                        Terrain = p.Name,
                        Terrain1 = p,
                        TokenType = "Жетон_власти",
                        Game = this.Id,
                        GameUserInfo = firstStep.GameUserInfo,
                        Id = Guid.NewGuid(),
                        Step = firstStep.Id
                    };
                    firstStep.GameUserInfo.PowerCounter.Add(powerCounter);
                }

                AddAllUnits(p, firstStep);
                AddAllUnits(p, lastStep, true);
            });
        }

        private void AddAllUnits(Terrain terrain, Step step, bool noPort = false)
        {
            UnitType knight = DbContext.UnitType.Single(p => p.Name == "Рыцарь");
            UnitType board = DbContext.UnitType.Single(p => p.Name == "Корабль");

            IEnumerable<Unit> units = step.GameUserInfo.Unit.Where(p => p.Terrain == terrain.Name);
            if (terrain.TerrainType == "Земля")
            {
                for (int i = 0; i < 4; i++)
                    new Unit(step.GameUserInfo, knight, terrain);
            }
            else
            {
                if (terrain.TerrainType == "Порт")
                {
                    if (!noPort)
                        for (int i = 0; i < 3; i++)
                            new Unit(step.GameUserInfo, board, terrain);
                }
                else
                    for (int i = 0; i < 4; i++)
                        new Unit(step.GameUserInfo, board, terrain);
            }
        }
        #endregion

        public void NewTurn()
        {
            this.GameHost.ArrowsData.Clear();

            this.Vesteros.LastStep.IsNew = false;
            Step newVesterosStep = this.Vesteros.LastStep.CopyStep("Событие_Вестероса", true);
            newVesterosStep.GameInfo.Turn++;
            newVesterosStep.NewMessage($"dynamic_newTurn*{newVesterosStep.GameInfo.Turn}");

            if (this.IsGarrisonUp) newVesterosStep.GameInfo.Garrison.ToList().ForEach(p => p.Strength++);

            this.LastHomeSteps.ForEach(p => p.IsNew = false);

            for (int i = 1; i < 4; i++)
            {
                if (this.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == i).All(p => p.IsFull))
                    ResetVesterosDesc(i);

                VesterosDecks vesterosDeck = this.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == i).First(p => !p.IsFull);
                vesterosDeck.IsFull = true;
                VesterosCardType vesterosCardType = vesterosDeck.VesterosCardType1;
                vesterosCardType.PlayCount++;

                if (vesterosCardType.BarbarianFlag && this.GameInfo.Barbarian < 12)
                {
                    this.GameInfo.Barbarian += 2;
                    newVesterosStep.NewMessage("dynamic_barbarianThreat*" + this.GameInfo.Barbarian);
                }

                if (vesterosCardType.Name == "Зима близко")
                    ResetVesterosDesc(i--);
            }

            if (this.GameInfo.Barbarian == 12)
            {
                newVesterosStep.NewMessage("dynamic_wildlingsAttack*" + this.GameInfo.Barbarian);
                NewVoting("Одичалые");
            }
            else
                NextVesterosCard();
        }

        /// <summary>
        /// Вскрытие карты одичалых
        /// </summary>
        /// <param name="isVictory">true Победа Ночного дозора</param>
        /// <param name="user">Виновник торжества</param>
        public void NewBarbarian(bool isVictory, GameUser user)
        {
            Step newVesterosStep = this.Vesteros.LastStep.CopyStep("Событие_Вестероса", true);//Default
            if (isVictory)
                newVesterosStep.GameInfo.Barbarian = 0;
            else
            {
                if (newVesterosStep.GameInfo.Barbarian < 4)
                    newVesterosStep.GameInfo.Barbarian = 0;
                else
                    newVesterosStep.GameInfo.Barbarian -= 4;
            }

            if (this.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == 4).All(p => p.IsFull))
                ResetVesterosDesc(4);

            VesterosDecks vesterosDecks = this.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == 4).First(p => !p.IsFull);
            vesterosDecks.IsFull = true;
            VesterosCardAction vesterosCardAction = vesterosDecks.VesterosCardType1.VesterosCardAction.Single();
            vesterosDecks.VesterosActionType = vesterosCardAction.VesterosActionType;
            vesterosDecks.VesterosActionType1 = vesterosCardAction.VesterosActionType1;

            //Победа одичалых:|низшая ставка |все прочие |Победа Ночного дозора:|высшая ставка 
            switch (vesterosDecks.VesterosCardType1.Name)
            {
                #region Разведчик-оборотень
                case "Разведчик-оборотень":
                    //Победа одичалых:|низшая ставка сбрасывает все доступные жетоны власти|все прочие сбрасывают по 2 доступных жетона власти (или всю доступную власть, если у них меньше 2 жетонов власти)|Победа Ночного дозора:|высшая ставка тут же возвращат в свой запас доступных жетонов власти всю ставку, сделанную на отражение этой атаки
                    if (isVictory)
                    {
                        //виновник возвращает свою ставку
                        Step newStep = user.LastStep.CopyStep("Default", true);
                        newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 2));
                        newStep.GameUserInfo.ChangePower(newStep.Voting.PowerCount);
                    }
                    else
                    {
                        //Изменяем количество доступной власти
                        foreach (GameUser item in this.VotedUsers)
                        {
                            if (item.LastStep.GameUserInfo.Power == 0)
                                continue;

                            Step newStep = item.LastStep.CopyStep("Default", true);

                            if (user == item)
                            {
                                newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 1));
                                newStep.GameUserInfo.ChangePower(-20);
                            }
                            else
                            {
                                newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 0));
                                newStep.GameUserInfo.ChangePower(-2);
                            }
                        }
                    }
                    break;
                #endregion

                #region Разбойники Гремучей Рубашки
                case "Разбойники Гремучей Рубашки":
                    //Победа одичалых:|низшая ставка отсупает на 2 деления назад по треку снабжения (не ниже нуля)|все прочие отступают на 1 деление назад по треку снабжения (не ниже нуля)|Победа Ночного дозора:|высшая ставка продвигается на 1 деление вперёд по треку снабжения (не выше 6)
                    if (isVictory)
                    {
                        //виновник продвигается по снабжению                        
                        if (user.LastStep.GameUserInfo.Supply < 6)
                        {
                            Step newStep = user.LastStep.CopyStep("Default", true);
                            newStep.GameUserInfo.Supply++;
                            newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 2));
                            newStep.NewMessage(string.Format("dynamic_voting*event_{0}*{1}", "supply", newStep.GameUserInfo.Supply));
                        }
                    }
                    else
                    {
                        //Изменяем отступают по снабжению
                        foreach (GameUser item in this.VotedUsers)
                        {
                            if (item.LastStep.GameUserInfo.Supply > 0)
                            {
                                Step newStep = item.LastStep.CopyStep("Default", true);
                                if (user == item)
                                {
                                    newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 1));
                                    if (newStep.GameUserInfo.Supply > 1)
                                        newStep.GameUserInfo.Supply -= 2;
                                    else
                                        newStep.GameUserInfo.Supply = 0;
                                }
                                else
                                {
                                    newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 0));
                                    newStep.GameUserInfo.Supply--;
                                }

                                newStep.NewMessage(string.Format("dynamic_voting*event_{0}*{1}", "supply", newStep.GameUserInfo.Supply));
                                if (newStep.CheckSupply() > 0)
                                    newStep.NewSupplyStep();
                            }
                        }
                    }

                    if (this.LastHomeSteps.Any(p => !p.IsFull)) return;
                    else break;
                #endregion

                #region Сбор на Молоководной
                case "Сбор на Молоководной":
                    //Победа одичалых:|низшая ставка, если держит на руке больше одной карты Дома, сбрасывает все карты с наибольшей боевой силой|все прочие, если держит на руке больше одной карты Дома, сбрасывают по 1 карте на свой выбор|Победа Ночного дозора:|забирает на руку весь сброс своих карт Дома
                    if (isVictory)
                    {
                        //забирает сброс карт
                        Step newStep = user.LastStep.CopyStep("Default", true);
                        newStep.GameUserInfo.UsedHomeCard.Clear();
                        newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 2));
                    }
                    else
                    {
                        //скидывают карты в сброс
                        foreach (GameUser item in this.VotedUsers)
                        {
                            IEnumerable<HomeCardType> notUsed = item.HomeType1.HomeCardType.Where(p => !item.LastStep.GameUserInfo.UsedHomeCard.Any(p1 => p1.HomeCardType1 == p));
                            if (notUsed.Count() < 2)
                                continue;

                            if (user == item)
                            {
                                Step newStep = item.LastStep.CopyStep("Default", true);
                                newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 1));
                                int maxStrength = notUsed.Max(p => p.Strength);
                                foreach (HomeCardType card in notUsed.Where(p => p.Strength == maxStrength).ToList())
                                {
                                    if (notUsed.Count() > 1)
                                    {
                                        new UsedHomeCard(newStep.GameUserInfo, card);
                                        newStep.NewMessage("dynamic_homeCardDiscards*hero_" + card.Name);
                                    }
                                }
                            }
                            else
                            {
                                Step newStep = item.LastStep.CopyStep("Сбор_на_Молоководной", false);
                                newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 0));
                                newStep.NewMessage("dynamic_homeCardSelect");
                                newStep.NewRaven();
                            }
                        }
                    }
                    if (this.LastHomeSteps.Any(p => !p.IsFull)) return;
                    else break;
                #endregion

                #region Наездники_на_мамонтах
                case "Наездники_на_мамонтах":
                    //Победа одичалых:|низшая ставка теряет 3 любых отряда|все прочие теряют по 2 любых отряда|Победа Ночного дозора:|может выбрать из своего сброса 1 карту Дома и забрать её на руку
                    if (isVictory)
                    {
                        if (user.LastStep.GameUserInfo.UsedHomeCard.Count != 0)
                        {
                            Step newStep = user.LastStep.CopyStep("Наездники_на_мамонтах", false);
                            newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 2));
                            newStep.NewRaven();
                        }
                    }
                    else
                    {
                        //теряют отряды
                        foreach (GameUser item in this.VotedUsers)
                        {
                            int unitCount = item == user ? 3 : 2;
                            if (item.LastStep.GameUserInfo.Unit.Count < unitCount)
                                unitCount = item.LastStep.GameUserInfo.Unit.Count;
                            if (unitCount == 0)
                                continue;

                            Step newStep = item.LastStep.CopyStep("Наездники_на_мамонтах_роспуск_войск", false);
                            newStep.NewRaven();
                            newStep.Raven.StepType = unitCount.ToString();

                            newStep.NewMarch();
                            foreach (Unit unit in newStep.GameUserInfo.Unit)
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

                            if (item == user)
                                newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 1));
                            else
                                newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 0));
                        }
                    }
                    if (this.LastHomeSteps.Any(p => !p.IsFull)) return;
                    else break;
                #endregion

                #region Король-за-Стеной
                case "Король-за-Стеной":
                    //Победа одичалых:|низшая ставка отсупает на последнее деление всех треков влияния|все прочие в порядке хода каждый игрок отсупает на последнее деление трека вотчины или двора (по своему выбору)|Победа Ночного дозора:|высшая ставка продвигается на первое деление любого трека влияния и забирает соответствующий жетон превосходства
                    if (isVictory)
                    {
                        Step newStep = user.LastStep.CopyStep("Король-за-Стеной", false);
                        newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 2));
                        newStep.NewRaven();
                        newStep.Raven.StepType = ChangeTrackEffect.First.ToString();
                    }
                    else
                    {
                        Step newStep = user.LastStep.CopyStep("Default", true);
                        newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 1));
                        List<GameUser> homeUser = this.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.ThroneInfluence > newStep.GameUserInfo.ThroneInfluence).ToList();
                        homeUser.ForEach(p => p.ChangeTrack("Железный_трон", ChangeTrackEffect.Up));
                        user.ChangeTrack("Железный_трон", ChangeTrackEffect.Last);
                        homeUser = this.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.BladeInfluence > newStep.GameUserInfo.BladeInfluence).ToList();
                        homeUser.ForEach(p => p.ChangeTrack("Валирийский_меч", ChangeTrackEffect.Up));
                        user.ChangeTrack("Валирийский_меч", ChangeTrackEffect.Last);
                        homeUser = this.HomeUsersSL.Where(p => p.LastStep.GameUserInfo.RavenInfluence > newStep.GameUserInfo.RavenInfluence).ToList();
                        homeUser.ForEach(p => p.ChangeTrack("Посыльный_ворон", ChangeTrackEffect.Up));
                        user.ChangeTrack("Посыльный_ворон", ChangeTrackEffect.Last);

                        newStep = this.VotedUsers.First().LastStep.CopyStep("Король-за-Стеной", false);
                        newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 0));
                        newStep.NewRaven();
                        newStep.Raven.StepType = ChangeTrackEffect.Last.ToString();
                    }

                    return;
                #endregion

                #region Убийцы_ворон
                case "Убийцы_ворон":
                    //Победа одичалых:|низшая ставка заменяет всех своих рыцарей доступными пешими воинами. Рыцари, которых некем заменить, гибнут|все прочие заменяют пешими воинами по 2 любых своих рыцаря. Рыцари, которых некем заменить, гибнут|Победа Ночного дозора:|высшая ставка может тут же заменить до 2 любых своих пеших воинов доступными рыцарями
                    if (isVictory)
                    {
                        UnitType knight = this.DbContext.UnitType.Single(p => p.Name == "Рыцарь");
                        int availableKnight = knight.Count - user.LastStep.GameUserInfo.Unit.Count(p => p.UnitType1 == knight);
                        int footManCount = user.LastStep.GameUserInfo.Unit.Count(p => p.UnitType == "Пеший_воин");
                        if (availableKnight > 0 && footManCount > 0)
                        {
                            Step newStep = user.LastStep.CopyStep("Убийцы_ворон", false);
                            newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 2));
                            newStep.NewRaven();
                            newStep.Raven.StepType = "Upgrade";

                            newStep.NewMarch();
                            foreach (Unit unit in newStep.GameUserInfo.Unit.Where(p => p.UnitType == "Пеший_воин").ToList())
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
                        }
                    }
                    else
                    {
                        foreach (GameUser item in this.VotedUsers)
                        {
                            int knightCount = item.LastStep.GameUserInfo.Unit.Count(p => p.UnitType == "Рыцарь");
                            if (knightCount > 0)
                            {
                                Step newStep = item.LastStep.CopyStep("Default", true);
                                newStep.NewRaven();
                                newStep.Raven.StepType = "Downgrade";

                                newStep.NewMarch();
                                newStep.March.SourceOrder = "Убийцы_ворон";
                                foreach (Unit unit in newStep.GameUserInfo.Unit.Where(p => p.UnitType == "Рыцарь").ToList())
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

                                if (newStep == user.LastStep)
                                {
                                    newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 1));
                                    newStep.March.MarchUnit.ToList().ForEach(p => p.UnitType = "Пеший_воин");
                                    ExtWCFStep.UpdateUnit(newStep);
                                }
                                else
                                {
                                    newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 0));
                                    if (knightCount <= 2)
                                    {
                                        newStep.March.MarchUnit.ToList().ForEach(p => p.UnitType = "Пеший_воин");
                                        ExtWCFStep.UpdateUnit(newStep);
                                    }
                                    else
                                        newStep = newStep.CopyStep("Убийцы_ворон", false);
                                }
                            }
                        }
                    }

                    if (this.LastHomeSteps.Any(p => !p.IsFull)) return;
                    else break;
                #endregion

                #region Передовой_отряд
                case "Передовой_отряд":
                    //Победа одичалых:|низшая ставка на свой выбор либо теряет 2 любых отряда, либо отступает на 2 деления по тому треку влияния, где у неё наилучшая позиция|все прочие не караются|Победа Ночного дозора:|высшая ставка не участвует в торгах (и в распределении наград и штрафов по итогам) за отражение атаки одичалых, которые тут же наступают снова с силой 6
                    if (isVictory)
                    {
                        newVesterosStep.GameInfo.Barbarian = 6;
                        newVesterosStep.NewMessage("dynamic_wildlingsAttack*" + this.GameInfo.Barbarian);
                        NewVoting("Одичалые", user);

                        Step newStep = user.LastStep.CopyStep("Default", true);
                        newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 2));
                    }
                    else
                    {
                        Step newStep = user.LastStep.CopyStep("Передовой_отряд", false);
                        newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 1));
                        newStep.NewMarch();
                        newStep.March.SourceOrder = "Передовой_отряд";

                        foreach (Unit unit in newStep.GameUserInfo.Unit)
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

                        KeyValuePair<int, string>[] Influence = new KeyValuePair<int, string>[]
                        {
                            new KeyValuePair<int,string>(newStep.GameUserInfo.ThroneInfluence,"Железный_трон"),
                            new KeyValuePair<int,string>(newStep.GameUserInfo.BladeInfluence, "Валирийский_меч"),
                            new KeyValuePair<int,string>(newStep.GameUserInfo.RavenInfluence,"Посыльный_ворон")
                        };
                        IGrouping<int, KeyValuePair<int, string>> maxInfluence = Influence.GroupBy(p => p.Key).OrderBy(p => p.Key).First();

                        newStep.NewRaven();
                        maxInfluence.Select(p => p.Value).ToList().ForEach(p => newStep.Raven.StepType += p + "|");
                    }

                    if (this.LastHomeSteps.Any(p => !p.IsFull)) return;
                    else break;
                #endregion

                #region Наступление_орды
                case "Наступление_орды":
                    //Победа одичалых:|НИЗШАЯ СТАВКА теряет 2 отряда в одном из своих замков или крепостей. Если таких отрядов нет, теряет 2 любых отряда|ВСЕ ПРОЧИЕ теряют по 1 любому отряду|Победа Ночного дозора:|ВЫСШАЯ СТАВКА может собрать войска по обычным правилам сбора в любом подвластном замке или крепости
                    if (isVictory)
                    {
                        if (user.LastStep.GameUserInfo.GameUserTerrain.Select(p => p.Terrain1).Count(p => p.Strength != 0) == 0)
                            break;

                        Step newStep = user.LastStep.CopyStep("Наступление_орды_Усиление_власти", false);
                        newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 2));
                        newStep.NewMarch();

                        newStep.GameUserInfo.Order.Clear();
                        foreach (Terrain terrain in newStep.GameUserInfo.GameUserTerrain.Select(p => p.Terrain1).Where(p => p.Strength != 0).ToList())
                        {
                            Order order = new Order
                            {
                                Id = Guid.NewGuid()
                            };
                            order.FirstId = order.Id;
                            order.Step = newStep.Id;
                            order.Game = newStep.Game;
                            order.Terrain = terrain.Name;
                            order.OrderType = "Усиление_власти_0_специальный";
                            newStep.GameUserInfo.Order.Add(order);
                        }
                    }
                    else
                    {
                        //теряют отряды
                        foreach (GameUser item in this.VotedUsers)
                        {
                            if (item.LastStep.GameUserInfo.Unit.Count == 0)
                                continue;

                            int unitCount = item == user ? 2 : 1;

                            Step newStep = item.LastStep.CopyStep("Default", true);
                            newStep.NewRaven();
                            newStep.Raven.StepType = unitCount.ToString();

                            newStep.NewMarch();
                            newStep.March.SourceOrder = "Наступление_орды";
                            foreach (Unit unit in newStep.GameUserInfo.Unit)
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

                            if (item == user)
                                newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 1));
                            else
                                newStep.NewMessage(string.Format("dynamic_wildlingsResult*event_{0}*dynamic_wildlingsCategory{1}", vesterosDecks.VesterosCardType1.Id, 0));

                            if (newStep.March.MarchUnit.Count <= unitCount)
                            {
                                newStep.March.MarchUnit.ToList().ForEach(p => p.UnitType = null);
                                ExtWCFStep.UpdateUnit(newStep);
                            }
                            else
                                newStep = newStep.CopyStep("Наступление_орды", false);
                        }
                    }

                    if (this.LastHomeSteps.Any(p => !p.IsFull)) return;
                    else break;
                    #endregion
            }

            NextVesterosCard();
        }

        public void ResetVesterosDesc(int decksNumber)
        {
            Step newVesterosStep = this.Vesteros.LastStep.CopyStep("Событие_Вестероса", true);//Default
            switch (decksNumber)
            {
                case 4:
                    newVesterosStep.NewMessage("dynamic_resetWildlingsDeck*text_wildlings");
                    break;
                default:
                    newVesterosStep.NewMessage("dynamic_resetDeck*text_vesteros*" + decksNumber);
                    break;
            }

            int rndRange = 1000;
            int deskRange = decksNumber * rndRange;
            List<VesterosDecks> vesterosDecks = this.VesterosDecks.Where(p => p.VesterosCardType1.DecksNumber == decksNumber).ToList();
            foreach (VesterosDecks item in vesterosDecks)
            {
                item.IsFull = false;
                item.VesterosActionType = null;
                item.VesterosActionType1 = null;
                do { item.Sort = (int)(GameHost.Rnd.NextDouble() * rndRange + deskRange); }
                while (vesterosDecks.Any(p => p != item && p.Sort == item.Sort));
            }
        }

        public void NextVesterosCard()
        {
            Step newVesterosStep = this.Vesteros.LastStep.CopyStep("Событие_Вестероса", true);//Default

            VesterosDecks vesterosDecks = this.VesterosDecks.FirstOrDefault(p => p.IsFull && string.IsNullOrEmpty(p.VesterosActionType));
            if (vesterosDecks == null)
            {
                this.NewThink();
                return;
            }

            VesterosCardType vesterosCardType = vesterosDecks.VesterosCardType1;

            if (vesterosCardType.VesterosCardAction.Count > 1)
            {
                GameUser user = null;
                switch (vesterosCardType.DecksNumber)
                {
                    case 1: user = this.HomeUsersSL.Single(p => p.LastStep.GameUserInfo.ThroneInfluence == 1); break;
                    case 2: user = this.HomeUsersSL.Single(p => p.LastStep.GameUserInfo.RavenInfluence == 1); break;
                    case 3: user = this.HomeUsersSL.Single(p => p.LastStep.GameUserInfo.BladeInfluence == 1); break;
                }

                Step newStep = user.LastStep.CopyStep("Событие_Вестероса", false);
                newStep.NewMessage("dynamic_planning*event_" + vesterosCardType.Id);
                newStep.VesterosAction = new VesterosAction() { Step = newStep.Id, Game = newStep.Game, Step1 = newStep, VesterosDecks = vesterosDecks.FirstId };
                return;
            }

            VesterosCardAction vesterosCardAction = vesterosCardType.VesterosCardAction.Single();
            vesterosDecks.VesterosActionType = vesterosCardAction.VesterosActionType;
            vesterosDecks.VesterosActionType1 = vesterosCardAction.VesterosActionType1;

            newVesterosStep.NewMessage("dynamic_vesterosEvent*actionType_" + vesterosCardAction.VesterosActionType);

            VesterosAction(vesterosDecks.VesterosActionType);
        }

        public void VesterosAction(string vesterosActionType)
        {
            switch (vesterosActionType)
            {
                case "Борьба за влияние":
                    {
                        NewVoting("Железный_трон");
                        return;
                    }

                case "Сбор войск":
                    {
                        NewSbor();
                        return;
                    }

                case "Снабжение войск":
                    if (!CalcSupply())
                        return;
                    break;

                case "Сбор власти":
                    foreach (GameUser user in this.HomeUsersSL)
                    {
                        Step newStep = user.LastStep.CopyStep("Default", true);

                        foreach (Terrain terrain in newStep.GameUserInfo.GameUserTerrain.Select(p => p.Terrain1))
                        {
                            int consolidatePower = terrain.ConsolidatePower(this);
                            if (terrain.TerrainType == "Порт")
                                consolidatePower++;
                            if (consolidatePower <= 0)
                                continue;

                            newStep.GameUserInfo.ChangePower(consolidatePower);
                            newStep.NewMessage(string.Format("dynamic_consolidatePower*terrain_{0}*{1}", terrain.Name, newStep.GameUserInfo.Power));
                        }
                    }
                    break;

                case "Нашествие одичалых":
                    NewVoting("Одичалые");
                    return;
            }

            NextVesterosCard();
        }

        private bool CalcSupply()
        {
            bool flag = true;
            foreach (GameUser user in this.HomeUsersSL)
            {
                int newSupply = user.LastStep.GameUserInfo.GameUserTerrain.Sum(p1 => p1.Terrain1.Supply);
                if (user.LastStep.GameUserInfo.Supply == newSupply)
                    continue;

                Step newStep = user.LastStep.CopyStep("Default", true);
                newStep.GameUserInfo.Supply = newStep.GameUserInfo.GameUserTerrain.Sum(p1 => p1.Terrain1.Supply);
                if (newStep.GameUserInfo.Supply > 6)
                    newStep.GameUserInfo.Supply = 6;
                newStep.NewMessage("dynamic_voting*event_supply*" + newStep.GameUserInfo.Supply);

                if (newStep.CheckSupply() > 0)
                {
                    newStep.NewSupplyStep();
                    flag = false;
                }
            }

            return flag;
        }

        public void NextVoting(string lastVotingTarget)
        {
            switch (lastVotingTarget)
            {
                case "Железный_трон":
                    this.NewVoting("Вотчины");
                    break;

                case "Вотчины":
                    this.NewVoting("Королевский_двор");
                    break;

                case "Королевский_двор":
                    this.NextVesterosCard();
                    break;

                case "Одичалые":
                    int sumPower = this.VotedUsers.Sum(p => p.LastStep.Voting.PowerCount);
                    NewBarbarian(sumPower < this.GameInfo.Barbarian ? false : true, this.VotedUsers.Single(p => p.HomeType == LastStep.Raven.StepType));
                    break;
            }
        }

        public void NewVoting(string votingTarget, GameUser expUser = null)
        {
            List<GameUser> votingUser = null;

            if (expUser != null)
            {
                Step newStep = expUser.LastStep.CopyStep("Default", true);
                newStep.Voting = null;

                votingUser = this.HomeUsersSL.Where(p => p != expUser).ToList();
            }
            else
                votingUser = this.HomeUsersSL;

            foreach (GameUser user in votingUser)
            {
                Step newStep = user.LastStep.CopyStep("Борьба_за_влияние", true);
                if (newStep.GameUserInfo.Power != 0)
                {
                    newStep = newStep.CopyStep("Борьба_за_влияние", false);
                    //newStep.IsFull = false;
                    newStep.NewMessage("dynamic_clashOfKings*voting_" + votingTarget);
                }
                new Voting(newStep, votingTarget);
            }

            if (this.LastHomeSteps.All(p => p.IsFull))
                GameHost.UpdateVoting(this, votingTarget);
        }

        public void NewSbor()
        {
            GameUser user = this.HomeUsersSL.FirstOrDefault(p => p.LastStep.StepType != "Усиление_власти_Вестерос");
            if (user == null)
            {
                NextVesterosCard();
                return;
            }

            Step newStep = user.LastStep.CopyStep("Усиление_власти_Вестерос", true);
            newStep.NewMarch();
            newStep.NewMessage("dynamic_planning*stepType_Усиление_власти_Вестерос");
            newStep.GameUserInfo.Order.Clear();

            foreach (Terrain terrain in newStep.GameUserInfo.GameUserTerrain.Select(p => p.Terrain1).Where(p => p.Strength != 0).ToList())
            {
                newStep = newStep.CopyStep("Усиление_власти_Вестерос", false);

                Order order = new Order
                {
                    Id = Guid.NewGuid(),
                    Step = newStep.Id,
                    Game = newStep.Game,
                    Terrain = terrain.Name,
                    OrderType = "Усиление_власти_0_специальный"
                };
                order.FirstId = order.Id;

                newStep.GameUserInfo.Order.Add(order);
            }

            //пропускает ход при отсутствие замков
            if (newStep.IsFull)
                NewSbor();
        }

        public void NewThink()
        {
            this.ThroneProgress = 0;
            this.Vesteros.LastStep.IsNew = false;
            Step step = Vesteros.LastStep.CopyStep("Замысел", true);
            step.NewMessage("dynamic_planning*stepType_Замысел");

            foreach (GameUser user in this.HomeUsersSL)
            {
                user.LastStep.IsNew = false;
                step = user.LastStep.CopyStep("Замысел", true);

                //включаем меч и поднимаем юнитов
                step.GameUserInfo.IsBladeUse = false;
                foreach (Unit item in step.GameUserInfo.Unit.ToList())
                    item.IsWounded = false;

                //создаём приказы
                step.GameUserInfo.NewThink();
                if (step.GameUserInfo.Order.Count != 0)
                    step = step.CopyStep("Замысел", false);
                //step.IsFull = false;

                if (!step.IsFull)
                    step.NewMessage("dynamic_planning*stepType_Замысел");
            }
        }

        public GameUser GetTerrainHolder(Terrain terrain)
        {
            return this.HomeUsersSL.SingleOrDefault(p => p.LastStep.GameUserInfo.GameUserTerrain.Any(p2 => p2.Terrain1 == terrain));
        }

        /// <summary>
        /// Удаляет владения у текущего хозяина
        /// </summary>
        /// <param name="terrain">територия</param>
        /// <param name="newHolder">наследник кораблей</param>
        private void RemoveTerrainHolder(Terrain terrain, GameUser newHolder = null)
        {
            if (terrain.TerrainType == "Порт")
                return;

            GameUser oldHolder = this.GetTerrainHolder(terrain);
            if (oldHolder == null)
                return;

            Step oldHolderStep = oldHolder.LastStep.CopyStep("Default", true);
            oldHolderStep.NewMessage("dynamic_powerLoses*terrain_" + terrain.Name);

            oldHolderStep.GameUserInfo.GameUserTerrain.Remove(oldHolderStep.GameUserInfo.GameUserTerrain.SingleOrDefault(p => p.Terrain1 == terrain));
            oldHolderStep.GameUserInfo.PowerCounter.Remove(oldHolderStep.GameUserInfo.PowerCounter.SingleOrDefault(p => p.Terrain1 == terrain));

            if (terrain.TerrainPort != null)
            {
                oldHolderStep.GameUserInfo.GameUserTerrain.Remove(oldHolderStep.GameUserInfo.GameUserTerrain.Single(p => p.Terrain1 == terrain.TerrainPort));
                oldHolderStep.GameUserInfo.PowerCounter.Remove(oldHolderStep.GameUserInfo.PowerCounter.SingleOrDefault(p => p.Terrain1 == terrain.TerrainPort));

                //захват кораблей в порту
                Step newHolderStep = null;
                foreach (Unit ship in oldHolderStep.GameUserInfo.Unit.Where(p => p.Terrain1 == terrain.TerrainPort).ToList())
                {
                    if (newHolder != null)
                    {
                        if (newHolderStep == null)
                        {
                            newHolderStep = newHolder.LastStep.CopyStep("Роспуск_войск", true);
                            newHolderStep.NewMarch();
                            newHolderStep.March.SourceOrder = "Захват_порта";
                            newHolderStep.NewMessage("dynamic_shipCouldDestroy*terrain_" + terrain.TerrainPort.Name);
                        }

                        if (newHolderStep.GameUserInfo.Unit.Count(p => p.UnitType == "Корабль") < 6)
                        {
                            newHolderStep = newHolderStep.CopyStep("Роспуск_войск", false);
                            //newHolderStep.IsFull = false;
                            ship.Step = newHolderStep.Id;
                            ship.GameUserInfo = newHolderStep.GameUserInfo;
                            newHolderStep.GameUserInfo.Unit.Add(ship);
                            newHolderStep.March.MarchUnit.Add(new MarchUnit()
                            {
                                Id = Guid.NewGuid(),
                                March = newHolderStep.March,
                                Step = newHolderStep.March.Step,
                                Terrain = ship.Terrain,
                                Unit = ship.FirstId,
                                UnitType = ship.UnitType
                            });
                            newHolderStep.NewMessage("dynamic_shipTaken");
                        }
                        else
                            newHolderStep.NewMessage("dynamic_shipDestroy");
                    }

                    oldHolderStep.GameUserInfo.Unit.Remove(ship);
                    oldHolderStep.GameUserInfo.Order.Remove(oldHolderStep.GameUserInfo.Order.SingleOrDefault(p => p.Terrain1 == terrain.TerrainPort));
                    oldHolderStep.NewMessage("dynamic_shipLost");
                }
            }
        }

        /// <summary>
        /// Удаляет владения у текущего хозяина
        /// </summary>
        /// <param name="terrain">територия</param>
        public void RemoveTerrainHolder(Terrain terrain)
        {
            this.RemoveTerrainHolder(terrain, null);
        }

        /// <summary>
        /// Добавляет владения новому хозяину удаляя у старого
        /// </summary>
        /// <param name="terrain">територия</param>
        /// <param name="newHolder">новый владелец</param>
        public void NewTerrainHolder(Terrain terrain, GameUser newHolder)
        {
            //исключаем захват своих владений
            if (newHolder == this.GetTerrainHolder(terrain))
                return;

            //уничтожаем гарнизон
            if (this.GameInfo.Garrison.Any(p => p.Terrain1 == terrain))
            {
                Step newVesterosStep = this.Vesteros.LastStep.CopyStep("Default", true);
                newVesterosStep.NewMessage("dynamic_garrisonRemove*terrain_" + terrain.Name);
                this.GameInfo.Garrison.Remove(this.GameInfo.Garrison.Single(p => p.Terrain1 == terrain));
            }

            //освобождаем владения
            this.RemoveTerrainHolder(terrain, newHolder);

            //добавляем владения
            newHolder.LastStep.GameUserInfo.NewGameUserTerrain(terrain);
            if (terrain.TerrainPort != null)
                newHolder.LastStep.GameUserInfo.NewGameUserTerrain(terrain.TerrainPort);

            //конец игры
            if (newHolder.LastStep.CastleCount > 6)
                this.TheEnd();
        }

        public void NewBattle(GameUser attackUser, Terrain attackTerrain, Terrain defenceTerrain, bool IsNeedSupport)
        {
            Step newVesterosStep = this.Vesteros.LastStep.CopyStep("Сражение", true);
            GameUser defenceUser = this.GetTerrainHolder(defenceTerrain);

            //Сражение
            Battle battle = new Battle
            {
                Id = Guid.NewGuid(),
                AttackTerrain = attackTerrain.Name,
                DefenceTerrain = defenceTerrain.Name,
                AttackUser = attackUser.Id,
                DefenceUser = defenceUser.Id,
                IsAttackUserNeedSupport = IsNeedSupport,

                Step = newVesterosStep.Id,
                Game = this.Id,
                GameInfo = this.GameInfo
            };
            this.GameInfo.Battle = battle;

            //Одновременное захват порта и нападение на другую территорию
            this.LastHomeSteps.Where(p => p.StepType == "Роспуск_войск" && p.March != null).ToList().ForEach(p => p.March.SourceOrder = "Захват_порта_до_сражения");

            if (this.LastHomeSteps.All(p => p.IsFull))
                this.GameInfo.Battle.Start();
        }

        internal void TheEnd()
        {
            this.LastHomeSteps.ForEach(p => p.IsNew = false);

            List<GameUser> users = this.LastHomeSteps.OrderByDescending(p => p.CastleCount)
                .ThenByDescending(p => p.LandCount)
                .ThenByDescending(p => p.GameUserInfo.Supply)
                .ThenBy(p => p.GameUserInfo.ThroneInfluence).Select(p => p.GameUser1).ToList();

            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].Login != null)
                    this.GameHost.GamePortalServer.StopUserGame(users[i].Login, this.Id, i + 1);

                Step newStep = users[i].LastStep.CopyStep("Победа", true);
                string pos = (i + 1).ToString();
                newStep.NewRaven();
                newStep.Raven.StepType = pos;
                if (i == 0)
                    newStep.NewMessage("dynamic_theEndWinner");
                else
                    newStep.NewMessage("dynamic_theEnd*" + pos);
            }

            GameHost.ArrowsData.AddToDbContext(this);
            this.CloseTime = DateTimeOffset.UtcNow;
            this.DbContext.SaveChanges();

            GameHost.ChatService.AddChat(new Chat() { Creator = "Вестерос", Message = $"dynamic_gameOver" });
            GameHost.AddGameNotifiFunc(this.ToWCFGame());


            if (Agot2Server.Service.IsDisableNewGame)
            {
                int gamesCount = this.DbContext.Game.Where(p => p.CreatorLogin != "System" && p.CloseTime == null).Count();
                WCFUser admin = this.GameHost.GamePortalServer.GetProfileByLogin("17a87d89-b8d7-4274-9049-78d7b6af94af");
                GameHost.UserInviteFunc(admin, "17a87d89-b8d7-4274-9049-78d7b6af94af", $"Игра '{this.Name}' завершена. Осталось {gamesCount} незавершённых игр");
            }

            throw new TheEndException();
        }
    }
}
