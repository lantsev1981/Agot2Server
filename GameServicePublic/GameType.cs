using System;
using System.Collections.Generic;

namespace GameService
{
    public class GameTypes : List<GameTypeItem>
    {
        public GameTypes()
        {
            this.Add(new GameTypeItem() { GameId = Guid.Empty, Name = "gameType_classic", ImageUri = "/Image/GameItemView/0.png", Id = 0, PlayerCount = 6 });
            this.Add(new GameTypeItem() { GameId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "gameType_dragon", ImageUri = "/Image/GameItemView/2.png", Id = 2, PlayerCount = 6 });

            this.Add(new GameTypeItem() { GameId = Guid.Parse("00000000-0000-0000-0000-000000000050"), Name = "gameType_classic", ImageUri = "/Image/GameItemView/4.png", Id = 4, PlayerCount = 5 });
            this.Add(new GameTypeItem() { GameId = Guid.Parse("00000000-0000-0000-0000-000000000040"), Name = "gameType_classic", ImageUri = "/Image/GameItemView/6.png", Id = 6, PlayerCount = 4 });
            this.Add(new GameTypeItem() { GameId = Guid.Parse("00000000-0000-0000-0000-000000000030"), Name = "gameType_classic", ImageUri = "/Image/GameItemView/8.png", Id = 8, PlayerCount = 3 });

            this.Add(new GameTypeItem() { GameId = Guid.Parse("00000000-0000-0000-0000-000000000250"), Name = "gameType_homerules_classic_1", ImageUri = "/Image/GameItemView/10.png", Id = 10, PlayerCount = 5 });
        }
    }

    public class GameTypeItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUri { get; set; }
        public Guid GameId { get; set; }
        public int PlayerCount { get; set; }
    }
}
