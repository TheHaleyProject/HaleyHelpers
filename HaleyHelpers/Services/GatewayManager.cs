using Haley.Abstractions;
using Haley.Internal;
using Haley.Models;
using Haley.Utils;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Haley.Services {
    public static class GatewayManager {
        private static readonly ConcurrentDictionary<string, APIGatewayInfoHolder> _gateways = new();

        private static string GetKey<T>() => typeof(T).FullName ?? typeof(T).Name;
        private static string GetKey(IAPIGateway gw) => gw.GetType().FullName ?? gw.GetType().Name;

        /// <summary>Gets a previously registered gateway by its concrete type.</summary>
        public static T GetGateway<T>() where T : class, IAPIGateway {
            var key = GetKey<T>();

            if (!_gateways.TryGetValue(key, out var holder))
                throw new InvalidOperationException($"Gateway with key {key} does not exist. Register it first.");

            if (holder.Instance is not T typed)
                throw new InvalidOperationException($"Gateway key {key} exists but is not of type {typeof(T).FullName}.");

            return typed;
        }

        /// <summary>
        /// Registers a gateway instance (or returns existing one).
        /// Safe to call multiple times; will keep the first instance and update SessionFactory if provided.
        /// </summary>
        public static T AddOrGetGateway<T>(this T gateway, Func<Task<IAPIGatewaySession>> sessionFactory)
            where T : class, IAPIGateway {
            if (gateway == null) throw new ArgumentNullException(nameof(gateway));
            if (sessionFactory == null) throw new ArgumentNullException(nameof(sessionFactory));

            // Use runtime type to avoid accidents when called through an interface variable.
            var key = GetKey(gateway);

            var holder = _gateways.GetOrAdd(key, _ => new APIGatewayInfoHolder {
                Instance = gateway,
                SessionFactory = sessionFactory
            });

            // If already registered, keep instance but allow updating the factory.
            holder.SessionFactory ??= sessionFactory;

            if (holder.Instance is not T typed)
                throw new InvalidOperationException($"Gateway key {key} exists but is not of type {typeof(T).FullName}.");

            return typed;
        }

        /// <summary>Ensures the session is valid (exists, has token, not expiring soon). Refreshes via SessionFactory if needed.</summary>
        public static Task<bool> EnsureToken<T>() where T : class, IAPIGateway
            => GetGateway<T>().EnsureToken();

        static bool IsValidSession(IAPIGatewaySession? session, DateTime now) => session != null && session.HasToken() && !session.IsExpiringSoon(now);

        public static async Task<bool> EnsureToken<T>(this T gateway) where T : class, IAPIGateway {
            if (gateway == null) return false;

            // Fast path
            if (IsValidSession(gateway.Session, DateTime.UtcNow)) return true;

            var key = GetKey(gateway);
            if (!_gateways.TryGetValue(key, out var holder))
                throw new InvalidOperationException($"Gateway with key {key} does not exist.");

            var factory = holder.SessionFactory
                ?? throw new InvalidOperationException($"No SessionFactory registered for gateway {key}.");

            await holder.Gate.WaitAsync().ConfigureAwait(false); ///One call enters the semaphore, rest everything waits here.
            try {
                // Double-check with fresh time snapshot, because the waiting threads will enter after the previous thread might have made the session valid.
                if (IsValidSession(gateway.Session, DateTime.UtcNow)) return true;

                // Only ONE thread reaches here -> safe to refresh
                var newSession = await factory().ConfigureAwait(false);
                if (newSession == null) return false;

                gateway.Session = newSession; //Replace.
                return newSession.HasToken();
            } finally {
                holder.Gate.Release(); //Release semaphore for waiting threads.
            }
        }

        public static async Task<IAPIGatewaySession?> GetSessionAsync<T>(this T gateway) where T : class, IAPIGateway {
            var ok = await gateway.EnsureToken().ConfigureAwait(false);
            return ok ? gateway.Session : null;
        }

        public static async Task<IAPIGatewayToken?> GetTokenAsync<T>(this T gateway) where T : class, IAPIGateway {
            var ok = await gateway.EnsureToken().ConfigureAwait(false);
            return ok ? gateway.Session?.Token : null;
        }
    }
}