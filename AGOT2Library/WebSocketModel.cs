using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Agot2Server
{
    public class WebSocketModel
    {
        public int Port { get; protected set; }
        public string Name { get; protected set; }
        public Guid GameId { get; protected set; }

        public virtual void Dispose() { }
    }
}
