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
    
    public partial class HomeCardType
    {
        public HomeCardType()
        {
            this.UsedHomeCard = new HashSet<UsedHomeCard>();
        }
    
        public string Name { get; set; }
        public string HomeType { get; set; }
        public int Strength { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public string Specialization { get; set; }
    
        public virtual ICollection<UsedHomeCard> UsedHomeCard { get; set; }
        public virtual HomeType HomeType1 { get; set; }
    }
}
