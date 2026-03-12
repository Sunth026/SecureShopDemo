using System.Collections.Concurrent;
using SecureShopDemo.Data;
using SecureShopDemo.Models;

namespace SecureShopDemo.Services
{
    public class DdosProtectionService
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _requests = new();
        private readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public DdosProtectionService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public bool IsBlocked(string ip)
        {
            if (_blockedIps.TryGetValue(ip, out var blockedUntil))
            {
                if (blockedUntil > DateTime.UtcNow)
                    return true;

                _blockedIps.TryRemove(ip, out _);
            }

            return false;
        }

        public void RecordRequest(string ip, string path, string method)
        {
            var now = DateTime.UtcNow;
            var requestList = _requests.GetOrAdd(ip, _ => new List<DateTime>());

            lock (requestList)
            {
                requestList.RemoveAll(x => x < now.AddSeconds(-10));
                requestList.Add(now);

                if (requestList.Count > 10)
                {
                    _blockedIps[ip] = now.AddMinutes(1);

                    SaveAttackLog(ip, path, method, "Too many requests in short time");

                    requestList.Clear();
                }
            }
        }

        private void SaveAttackLog(string ip, string path, string method, string description)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.AttackLogs.Add(new AttackLog
            {
                IpAddress = ip,
                AttackType = "DDoS Suspected",
                Path = path,
                Method = method,
                Description = description,
                CreatedAt = DateTime.Now
            });

            context.SaveChanges();
        }

        public Dictionary<string, DateTime> GetBlockedIps()
        {
            var now = DateTime.UtcNow;

            return _blockedIps
                .Where(x => x.Value > now)
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}