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
    
    public partial class TerrainTerrain
    {
        public System.Guid Id { get; set; }
        public string Terrain { get; set; }
        public string JoinTerrain { get; set; }
    
        public virtual Terrain Terrain1 { get; set; }
        public virtual Terrain Terrain2 { get; set; }
    }
}
