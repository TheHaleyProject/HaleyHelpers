using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Haley.Abstractions;

namespace Haley.Internal {
    internal sealed class APIGatewayInfoHolder {
        public SemaphoreSlim Gate { get; } = new SemaphoreSlim(1, 1);
        public IAPIGateway Instance { get; set; } = default!;
        public IGatewaySessionProvider Provider { get; set; } = default!;
        // Anti-spam
        public DateTime? LastNotifyUtc { get; set; }
    }
}
