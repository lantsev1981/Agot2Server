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
    
    public partial class Garrison
    {
        public System.Guid Id { get; set; }
        public int Step { get; set; }
        public System.Guid Game { get; set; }
        public string TokenType { get; set; }
        public string Terrain { get; set; }
        public int Strength { get; set; }
    
        public virtual GameInfo GameInfo { get; set; }
        public virtual Terrain Terrain1 { get; set; }
        public virtual TokenType TokenType1 { get; set; }
    }
}
