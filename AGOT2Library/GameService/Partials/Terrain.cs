using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace GameService
{
    public partial class Terrain
    {
        public WCFTerrain ToWCFTerrain()
        {
            WCFTerrain result = new WCFTerrain();
            result.Name = this.Name;
            result.TerrainType = this.TerrainType;
            result.Supply = this.Supply;
            result.Power = this.Power;
            result.Strength = this.Strength;

            return result;
        }

        public Terrain TerrainPort
        {
            get
            {
                if (this.TerrainType == "Земля")
                {
                    TerrainTerrain terrainPort = this.TerrainTerrain.SingleOrDefault(p => p.JoinTerrain.Contains("Порт"));
                    return terrainPort == null ? null : terrainPort.Terrain2;
                }

                return null;
            }
        }

        /// <summary>
        /// Возвращает количество возможных жетонов власти при сборе власти
        /// </summary>
        /// <param name="gameData"></param>
        /// <param name="game"></param>
        /// <returns>-1 если нельзя собирать власть</returns>
        public int ConsolidatePower(Game game)
        {
            if (this.TerrainType == "Земля")
                return this.Power;

            if (this.TerrainType == "Порт")
            {
                GameUser terrainHolder = game.GetTerrainHolder(this);
                if (terrainHolder.LastStep.GameUserInfo.Unit.Any(p => p.Terrain1 == this))
                {
                    //в блокированном порту не действует
                    Terrain joinTerrain = this.TerrainTerrain.Single(p => p.Terrain2.TerrainType == "Море").Terrain2;
                    GameUser joinTerrainHolder = game.GetTerrainHolder(joinTerrain);
                    if (joinTerrainHolder == null || joinTerrainHolder == terrainHolder)
                        return 0;
                }
            }

            return -1;
        }

        public List<Terrain> GetRetreatTerrain(Game game, GameUser user, bool isLand, List<Terrain> checkedSea = null)
        {
            var result = new List<Terrain>();
            if (checkedSea == null)
                checkedSea = new List<Terrain>();

            foreach (var item in this.TerrainTerrain1.Select(p => p.Terrain1).ToList())
            {
                var holder = game.GetTerrainHolder(item);
                //Если чужая земля
                if ((holder != null && holder != user) || (holder == null && game.GameInfo.Garrison.Any(p => p.Terrain1 == item)))
                    continue;

                if (isLand)
                {
                    if (item.TerrainType == "Порт")
                        continue;
                    if (item.TerrainType == "Море")
                    {
                        //если не своё море
                        if (holder == null)
                            continue;
                        //Если уже проверяли
                        if (checkedSea.Contains(item))
                            continue;
                        else
                            checkedSea.Add(item);

                        result.AddRange(item.GetRetreatTerrain(game, user, isLand, checkedSea));
                    }
                    else
                        result.Add(item);
                }
                else
                {
                    if (item.TerrainType == "Земля")
                        continue;
                    if (item.TerrainType == "Порт" && holder == null)
                        continue;

                    result.Add(item);
                }
            }

            return result.Distinct().ToList();
        }
    }
}
