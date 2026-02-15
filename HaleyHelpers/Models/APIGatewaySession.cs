using System;
using Haley.Abstractions;

namespace Haley.Models {
    public sealed class APIGatewaySession : IAPIGatewaySession {
        public IAPIGatewayToken? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public double RefreshMinutesBeforeExpiry { get; set; } = 10;

        public APIGatewaySession() { }

        public APIGatewaySession( IAPIGatewayToken? token,DateTime? expiresAt,  double refreshMinutesBeforeExpiry = 10) {
            Token = token;
            ExpiresAt = expiresAt;
            RefreshMinutesBeforeExpiry = refreshMinutesBeforeExpiry;
        }

        public bool HasToken() => Token != null;

        public bool IsExpiringSoon(DateTime utcNow)
            => !ExpiresAt.HasValue || (ExpiresAt.Value - utcNow).TotalMinutes <= RefreshMinutesBeforeExpiry;
    }
}