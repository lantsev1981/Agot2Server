using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamePortal
{
    public partial class UserGame
    {
        internal WCFUserGame ToWCFUserGame()
        {
            WCFUserGame result = new WCFUserGame
            {
                Id = this.Id,
                GameId = this.GameId,
                GameType = this.GameType,
                HomeType = this.HomeType,
                StartTime = this.StartTime,
                EndTime = this.EndTime,
                MindPosition = this.MindPosition,
                IsIgnoreMind = this.IsIgnoreMind,
                HonorPosition = this.HonorPosition,
                IsIgnoreHonor = this.IsIgnoreHonor,
                IsIgnoreDurationHours = this.IsIgnoreDurationHours
            };

            return result;
        }
    }
}
