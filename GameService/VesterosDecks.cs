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
    
    public partial class VesterosDecks
    {
        public System.Guid Id { get; set; }
        public System.Guid FirstId { get; set; }
        public int Step { get; set; }
        public System.Guid Game { get; set; }
        public string VesterosCardType { get; set; }
        public bool IsFull { get; set; }
        public string VesterosActionType { get; set; }
        public int Sort { get; set; }
    
        public virtual GameInfo GameInfo { get; set; }
        public virtual VesterosActionType VesterosActionType1 { get; set; }
        public virtual VesterosCardType VesterosCardType1 { get; set; }
    }
}