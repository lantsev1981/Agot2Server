//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GamePortal
{
    using System;
    using System.Collections.Generic;
    
    public partial class Payment
    {
        public System.Guid Id { get; set; }
        public string Login { get; set; }
        public System.DateTimeOffset Time { get; set; }
        public int Power { get; set; }
        public string Event { get; set; }
        public string Comment { get; set; }
        public bool IsPublic { get; set; }
    
        public virtual User User { get; set; }
    }
}