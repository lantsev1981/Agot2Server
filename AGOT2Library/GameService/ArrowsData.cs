using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameService
{
    public partial class Arrow
    {
        public Arrow() { }

        public Arrow(ArrowModel item)
        {
            this.id = Guid.NewGuid();
            this.FirstId = item.FirstId;
            this.StartTerrainName = item.StartTerrainName;
            this.EndTerrainName = item.EndTerrainName;
            this.ArrowType = (int)item.ArrowType;
        }

        public ArrowModel ToArrowModel()
        {
            var result = new ArrowModel();
            result.FirstId = this.FirstId;
            result.StartTerrainName = this.StartTerrainName;
            result.EndTerrainName = this.EndTerrainName;
            result.ArrowType = (ArrowType)this.ArrowType;

            return result;
        }
    }

    public class ArrowList : List<ArrowModel>
    {
        public ArrowList(Guid gameId)
        {
            using (var dbContext = new Agot2p6Entities())
            {
                var game = dbContext.Game.SingleOrDefault(p => p.Id == gameId);
                this.AddRange(game.LastStep.Arrow.ToList().Select(p => p.ToArrowModel()));
            }
        }

        new public void Insert(int index, ArrowModel item) { }
        new public void InsertRange(int index, IEnumerable<ArrowModel> collection) { }
        new public void AddRange(IEnumerable<ArrowModel> collection) { collection.ToList().ForEach(p => this.Add(p)); }

        new public void Add(ArrowModel item)
        {
            if (item.StartTerrainName == item.EndTerrainName)
                throw new Exception("ArrowsList: StartTerrainName == EndTerrainName");
            if (this.Any(p => p.StartTerrainName == item.StartTerrainName && p.EndTerrainName == item.EndTerrainName))
                return;

            base.Add(item);
        }

        public void AddToDbContext(Game game)
        {
            var newStep = game.LastHomeSteps.Where(p1 => p1.IsNew).ToList();

            if (game.Vesteros.LastStep.IsNew)
                newStep.Add(game.Vesteros.LastStep);

            newStep.ForEach(p => this.ForEach(p1 => p.Arrow.Add(new Arrow(p1))));
        }
    }
}
