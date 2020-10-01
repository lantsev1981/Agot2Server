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
    
    public partial class Game
    {
        public Game()
        {
            this.GameUser = new HashSet<GameUser>();
        }
    
        public System.Guid Id { get; set; }
        public string CreatorLogin { get; set; }
        public System.DateTimeOffset CreateTime { get; set; }
        public Nullable<System.DateTimeOffset> OpenTime { get; set; }
        public Nullable<System.DateTimeOffset> CloseTime { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public int StepIndex { get; set; }
        public int MessageIndex { get; set; }
        public int Type { get; set; }
        public int MindRate { get; set; }
        public int HonorRate { get; set; }
        public int LikeRate { get; set; }
        public int DurationRate { get; set; }
        public int ThroneProgress { get; set; }
        public int RandomIndex { get; set; }
        public bool IsRandomSkull { get; set; }
        public bool IsDeleteIgnore { get; set; }
    
        public virtual ICollection<GameUser> GameUser { get; set; }
    }
}
