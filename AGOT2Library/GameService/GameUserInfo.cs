//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GameService
{
    using System;
    using System.Collections.Generic;
    
    public partial class GameUserInfo
    {
        public GameUserInfo()
        {
            this.GameUserTerrain = new HashSet<GameUserTerrain>();
            this.Order = new HashSet<Order>();
            this.PowerCounter = new HashSet<PowerCounter>();
            this.Unit = new HashSet<Unit>();
            this.UsedHomeCard = new HashSet<UsedHomeCard>();
        }
    
        public int Step { get; set; }
        public System.Guid Game { get; set; }
        public int Power { get; set; }
        public int Supply { get; set; }
        public int ThroneInfluence { get; set; }
        public int BladeInfluence { get; set; }
        public int RavenInfluence { get; set; }
        public bool IsBladeUse { get; set; }
    
        public virtual Step Step1 { get; set; }
        public virtual ICollection<GameUserTerrain> GameUserTerrain { get; set; }
        public virtual ICollection<Order> Order { get; set; }
        public virtual ICollection<PowerCounter> PowerCounter { get; set; }
        public virtual ICollection<Unit> Unit { get; set; }
        public virtual ICollection<UsedHomeCard> UsedHomeCard { get; set; }
    }
}
