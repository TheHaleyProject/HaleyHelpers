using Haley.Abstractions;
using Haley.Enums;
using Haley.Internal;
using Haley.Models;
using Haley.Utils;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Haley.Services {
    public static class GatewayManager {
        private static readonly ConcurrentDictionary<string, APIGatewayInfoHolder> _gateways = new ConcurrentDictionary<string, APIGatewayInfoHolder>();

        private static string GetKey<T>() => typeof(T).FullName ?? typeof(T).Name;
        private static string GetKey(IAPIGateway gw) => gw.GetType().FullName ?? gw.GetType().Name;

        public static T AddOrGetGateway<T>(IGatewaySessionProvider provider) where T : class, IAPIGateway => AddOrGetGateway(Activator.CreateInstance<T>(), provider); //Create a new instance and register it. If already exists, return the existing one.

        /// <summary>
        /// Register (or return existing) gateway by concrete type. One instance per type key.
        /// </summary>
        public static T AddOrGetGateway<T>(this T gateway, IGatewaySessionProvider provider)
            where T : class, IAPIGateway {
            if (gateway == null) throw new ArgumentNullException(nameof(gateway));
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var key = GetKey(gateway);

            var holder = _gateways.GetOrAdd(key, _ => new APIGatewayInfoHolder {
                Instance = gateway,
                Provider = provider
            });

            // If already registered, keep existing instance but ensure Provider exists.
            if (holder.Provider == null) holder.Provider = provider;

            if (holder.Instance is not T typed)
                throw new InvalidOperationException($"Gateway key {key} exists but is not of type {typeof(T).FullName}.");

            return typed;
        }

        public static T GetGateway<T>() where T : class, IAPIGateway {
            var key = GetKey<T>();
            if (!_gateways.TryGetValue(key, out var holder))
                throw new InvalidOperationException($"Gateway with key {key} does not exist. Register it first.");

            if (holder.Instance is not T typed)
                throw new InvalidOperationException($"Gateway key {key} exists but is not of type {typeof(T).FullName}.");

            return typed;
        }

        static bool IsSessionValid(IAPIGatewaySession s, DateTime utcNow) => s != null && s.HasToken() && !s.IsExpiringSoon(utcNow);

        /// <summary>
        /// Ensure session is usable. This is the main method your API calls should use.
        /// </summary>
        static async Task<GatewaySessionResult> EnsureSessionAsync<T>(this T gateway)
            where T : class, IAPIGateway {
            if (gateway == null) return GatewaySessionResult.Failed("Gateway is null.");

            var now = DateTime.UtcNow;
            // Fast path: session already good
            if (IsSessionValid(gateway.Session,now)) return GatewaySessionResult.Valid(gateway.Session);

            var key = GetKey(gateway);
            if (!_gateways.TryGetValue(key, out var holder))
                throw new InvalidOperationException($"Gateway with key {key} does not exist.");

            var provider = holder.Provider ?? throw new InvalidOperationException($"No provider registered for gateway {key}.");

            // Optional: attempt a pure load from DB/cache before locking
            var loaded = await provider.TryLoadAsync(gateway); //May be the gateway has already completed all relevant actions and have loaded the session, so we can avoid locking and provider calls if it's already there and valid.
            
            if (loaded != null && IsSessionValid(loaded, now)) {
                gateway.Session = loaded;
                return GatewaySessionResult.Valid(loaded);
            }

            await holder.Gate.WaitAsync().ConfigureAwait(false); //Enter the semaphore to generate the session.
            try {
                now = DateTime.UtcNow;

                // Double-check after lock
                if (IsSessionValid(gateway.Session, now)) return GatewaySessionResult.Valid(gateway.Session);

                // Ensure via provider (may refresh automatically or may require user action)
                var result = await provider.EnsureAsync(gateway).ConfigureAwait(false);

                // If provider returned a session, attach it
                if (result.Session != null) gateway.Session = result.Session;
                return result; //It can be valid or refreshed or anything else.. pay attention here.
            } finally {
                holder.Gate.Release();
            }
        }

        /// <summary>
        /// Convenience: return token object (not string). Returns null if not usable.
        /// </summary>
        public static async Task<object?> GetTokenAsync<T>(this T gateway)
            where T : class, IAPIGateway {
            var res = await gateway.EnsureSessionAsync().ConfigureAwait(false);
            return (res.Session != null && res.Session.HasToken()) ? res.Session.Token : null;
        }

        public static Task<GatewaySessionResult> GetStatus<T>(this T gateway) where T : class, IAPIGateway => gateway.EnsureSessionAsync();

        /// <summary>
        /// Background/proactive monitor: call this periodically (e.g., every few hours).
        /// It can notify before expiry even when token is still valid.
        /// </summary>
        public static async Task MonitorAsync(TimeSpan notifyBeforeExpiry,TimeSpan notifyCooldown) {
            foreach (var kv in _gateways) {
                var now = DateTime.UtcNow;
                var holder = kv.Value;
                var gateway = holder.Instance;
                var provider = holder.Provider;

                if (gateway == null || provider == null) continue;

                var s = gateway.Session;
                if (s == null || !s.HasToken() || !s.ExpiresAtUtc.HasValue) continue; //if there is not token, then may it is not yet initialized. Like, a gateway is registered but not required or initialied. May be it was never configured but only registered , so it has no session at all. In this case, we can skip it until next time when it may be initialized.

                //Anti spam
                if (holder.LastNotifyUtc.HasValue && (now - holder.LastNotifyUtc.Value) < notifyCooldown) continue; //We are still in cool down phase.. dont' spam by sending notifications.

                var timeLeft = s.ExpiresAtUtc.Value - now; //How long do we have till we expire? We can notify if it's less than the configured threshold, even if it's still valid. This allows proactive user action before hitting an expired state.
               
                if (timeLeft <= notifyBeforeExpiry && timeLeft > TimeSpan.Zero) { //it timeleft is negative, then it is already expired, so we can skip this case and let the next case handle it. We only want to notify about upcoming expiry here.
                    await provider.NotifyAsync(gateway,GatewayNotifyReason.SessionExpiringSoon, $"Session expires in {timeLeft.TotalHours:F1} hours.").ConfigureAwait(false);
                    holder.LastNotifyUtc = now;
                } else if (timeLeft <= TimeSpan.Zero) {
                    // Expired case (optional)
                    await provider.NotifyAsync(gateway,GatewayNotifyReason.SessionExpired,"Session has expired.").ConfigureAwait(false);
                    holder.LastNotifyUtc = now;
                }
            }
        }
    }
}