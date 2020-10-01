using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePortal
{
    /// <summary>
    /// Счётчик онлайн игроков
    /// </summary>
    public partial class OnlineCounterModel
    {
        public List<OnlineCounterItemModel> Items { get; set; }

        /// <summary>
        /// Максимальное значение счётчика
        /// </summary>
        public OnlineCounterItemModel MaxItem { get; set; }

        /// <summary>
        /// текущее значение
        /// </summary>
        public OnlineCounterItemModel LastItem { get { return Items.Last(); } }
    }

    /// <summary>
    /// Модель данных
    /// количество игроков онлайн на момент времени
    /// </summary>
    public partial class OnlineCounterItemModel
    {
        /// <summary>
        /// Количество игроков онлайн
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// Момент времени
        /// </summary>
        public DateTimeOffset DateTime { get; set; }
    }
    
    public partial class Profile
    {
        public Profile()
        {
            Titles = new List<string>();
            AllPower = -100;
        }

        public string Id { get; set; }
        public List<string> Titles { get; set; }
        public int AllPower { get; set; }
        public string FIO { get; set; }
    }
}
