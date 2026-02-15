using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Haley.Abstractions;

namespace Haley.Internal {
    internal class APIGatewayInfoHolder  {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public Func<Task<IAPIGatewaySession>>? SessionFactory { get; set; }
        public IAPIGateway Instance { get; set; } = default!;
    }
}
