using System;
using System.Collections.Generic;
using System.Linq;
using GTANetworkAPI;

namespace AlarmSystem
{
    // -------------------------------------------------------
    // Alarm brands & subscription tiers
    // -------------------------------------------------------

    public static class AlarmBrands
    {
        public class Brand
        {
            public string Name { get; set; }
            public string Tier { get; set; }
            public int MonthlyPrice { get; set; }
            public float DetectionRadius { get; set; }
            public int CooldownSeconds { get; set; }
            public string Description { get; set; }
        }

        public static readonly Dictionary<string, Brand> All = new Dictionary<string, Brand>(StringComparer.OrdinalIgnoreCase)
        {
            ["SENTINEL"] = new Brand
            {
                Name = "Sentinel Basic",
                Tier = "Basic",
                MonthlyPrice = 500,
                DetectionRadius = 8f,
                CooldownSeconds = 120,
                Description = "Entry-level alarm. Standard range, slow response."
            },
            ["GUARDIAN"] = new Brand
            {
                Name = "Guardian Home",
                Tier = "Standard",
                MonthlyPrice = 1200,
                DetectionRadius = 12f,
                CooldownSeconds = 90,
                Description = "Reliable mid-range system. Good for residential."
            },
            ["VIPER"] = new Brand
            {
                Name = "Viper Pro",
                Tier = "Advanced",
                MonthlyPrice = 2500,
                DetectionRadius = 15f,
                CooldownSeconds = 60,
                Description = "Fast detection, reduced cooldown, wider coverage."
            },
            ["NEXUS"] = new Brand
            {
                Name = "Nexus Smart",
                Tier = "Premium",
                MonthlyPrice = 4500,
                DetectionRadius = 18f,
                CooldownSeconds = 45,
                Description = "Smart alerts with priority dispatch routing."
            },
            ["FORTRESS"] = new Brand
            {
                Name = "Fortress Elite",
                Tier = "Elite",
                MonthlyPrice = 8000,
                DetectionRadius = 22f,
                CooldownSeconds = 30,
                Description = "Maximum coverage, instant response, top-tier system."
            },
            ["PHANTOM"] = new Brand
            {
                Name = "Phantom Stealth",
                Tier = "Black",
                MonthlyPrice = 15000,
                DetectionRadius = 28f,
                CooldownSeconds = 15,
                Description = "Silent detection. Intruder is unaware alarm was triggered."
            }
        };
    }

    // -------------------------------------------------------
    // Data models
    // -------------------------------------------------------

    public class Property
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public string OwnerId { get; set; }
        public bool AlarmInstalled { get; set; }
        public string AlarmBrand { get; set; }
        public bool AlarmArmed { get; set; }
        public DateTime? AlarmTriggeredAt { get; set; }
        public DateTime? InstalledAt { get; set; }

        public float DetectionRadius => AlarmInstalled && AlarmBrands.All.TryGetValue(AlarmBrand ?? "", out var b) ? b.DetectionRadius : 0f;
        public int CooldownSeconds => AlarmInstalled && AlarmBrands.All.TryGetValue(AlarmBrand ?? "", out var b) ? b.CooldownSeconds : 60;

        public Property(int id, string name, Vector3 position, string ownerId)
        {
            Id = id;
            Name = name;
            Position = position;
            OwnerId = ownerId;
            AlarmInstalled = false;
        }
    }

    public class AlarmEvent
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; }
        public string IntruderId { get; set; }
        public string AlarmBrand { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Resolved { get; set; }

        public AlarmEvent(int propertyId, string propertyName, string intruderId, string brand)
        {
            PropertyId = propertyId;
            PropertyName = propertyName;
            IntruderId = intruderId;
            AlarmBrand = brand;
            Timestamp = DateTime.UtcNow;
            Resolved = false;
        }
    }

    public static class SecurityGroups
    {
        public static readonly List<string> NotifiedFactions = new List<string>
        {
            "LSSD", "LSPD", "ProTech Security"
        };
    }

    // -------------------------------------------------------
    // Main resource
    // -------------------------------------------------------

    public class AlarmSystem : Script
    {
        private static readonly Dictionary<int, Property> Properties = new Dictionary<int, Property>();
        private static readonly List<AlarmEvent> AlarmLog = new List<AlarmEvent>();
        private static readonly HashSet<string> PlayersInsideProperties = new HashSet<string>();
        private static int _nextPropertyId = 1;
        private const float CHECK_INTERVAL_MS = 2000f;

        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart()
        {
            NAPI.Util.ConsoleOutput("[AlarmSystem] Starting...");

            RegisterProperty("Vinewood Hills Villa", new Vector3(1394.7f, 1128.6f, 114.3f), "ExampleOwner");
            RegisterProperty("Del Perro Penthouse", new Vector3(-1471.7f, -545.8f, 56.2f), "AnotherOwner");
            RegisterProperty("Mirror Park Residence", new Vector3(1171.2f, -722.9f, 57.8f), "ThirdOwner");

            NAPI.Util.StartThread(ProximityCheckLoop);
            NAPI.Util.ConsoleOutput($"[AlarmSystem] Started. {Properties.Count} properties registered.");
        }

        // -------------------------------------------------------
        // Property registration
        // -------------------------------------------------------

        public static int RegisterProperty(string name, Vector3 position, string ownerId)
        {
            int id = _nextPropertyId++;
            Properties[id] = new Property(id, name, position, ownerId);
            return id;
        }

        [Command("createproperty", GreedyArg = true)]
        public void CreatePropertyCommand(Client player, string name)
        {
            if (!IsAdmin(player)) { player.SendChatMessage("~r~No permission."); return; }
            int id = RegisterProperty(name, player.Position, "unassigned");
            player.SendChatMessage($"~g~Property '{name}' created with ID #{id}.");
        }

        [Command("setowner")]
        public void SetOwnerCommand(Client player, int propertyId, string ownerName)
        {
            if (!IsAdmin(player)) { player.SendChatMessage("~r~No permission."); return; }
            if (!Properties.TryGetValue(propertyId, out Property prop)) { player.SendChatMessage("~r~Property not found."); return; }
            prop.OwnerId = ownerName;
            player.SendChatMessage($"~g~Property #{propertyId} owner set to {ownerName}.");
        }

        // -------------------------------------------------------
        // /secinstall — owner installs alarm brand
        // -------------------------------------------------------

        [Command("secinstall", GreedyArg = true)]
        public void SecInstallCommand(Client player, string brandKey = "")
        {
            Property prop = GetPropertyByOwner(player.SocialClubName);

            if (prop == null)
            {
                player.SendChatMessage("~r~You don't own any registered property.");
                return;
            }

            if (string.IsNullOrWhiteSpace(brandKey))
            {
                player.SendChatMessage("~w~--- Available Alarm Brands ---");
                foreach (var kv in AlarmBrands.All)
                {
                    var b = kv.Value;
                    player.SendChatMessage($"~b~{kv.Key.ToUpper()} ~w~| {b.Name} ~y~[{b.Tier}] ~g~${b.MonthlyPrice:N0}/mo ~w~| {b.Description}");
                }
                player.SendChatMessage("~w~Usage: ~b~/secinstall [brand]");
                return;
            }

            if (!AlarmBrands.All.TryGetValue(brandKey, out var brand))
            {
                player.SendChatMessage($"~r~Unknown brand '{brandKey}'. Use /secinstall to see available brands.");
                return;
            }

            prop.AlarmInstalled = true;
            prop.AlarmBrand = brandKey.ToUpper();
            prop.AlarmArmed = true;
            prop.InstalledAt = DateTime.UtcNow;

            player.SendChatMessage($"~g~[INSTALLED] ~w~{brand.Name} ~y~({brand.Tier}) ~w~installed at {prop.Name}.");
            player.SendChatMessage($"~w~Monthly subscription: ~g~${brand.MonthlyPrice:N0} ~w~| Detection radius: ~b~{brand.DetectionRadius}m ~w~| Alarm is now ~g~ARMED~w~.");
        }

        // -------------------------------------------------------
        // /secuninstall — owner removes alarm
        // -------------------------------------------------------

        [Command("secuninstall")]
        public void SecUninstallCommand(Client player)
        {
            Property prop = GetPropertyByOwner(player.SocialClubName);

            if (prop == null) { player.SendChatMessage("~r~You don't own any registered property."); return; }
            if (!prop.AlarmInstalled) { player.SendChatMessage("~r~No alarm installed on your property."); return; }

            string oldBrand = prop.AlarmBrand;
            prop.AlarmInstalled = false;
            prop.AlarmBrand = null;
            prop.AlarmArmed = false;
            prop.AlarmTriggeredAt = null;

            player.SendChatMessage($"~y~[UNINSTALLED] ~w~Alarm system removed from {prop.Name}. Brand: {oldBrand}.");
        }

        // -------------------------------------------------------
        // /alarmstatus — shows all properties and their alarm state
        // -------------------------------------------------------

        [Command("alarmstatus")]
        public void AlarmStatusCommand(Client player)
        {
            if (!IsSecurityPersonnel(player) && !IsAdmin(player)) { player.SendChatMessage("~r~No permission."); return; }

            player.SendChatMessage("~w~--- Property Alarm Status ---");
            foreach (var prop in Properties.Values)
            {
                if (!prop.AlarmInstalled)
                {
                    player.SendChatMessage($"~w~#{prop.Id} {prop.Name} ~r~[NO ALARM]");
                    continue;
                }

                string status = prop.AlarmArmed ? "~g~ARMED" : "~y~DISARMED";
                bool recentTrigger = prop.AlarmTriggeredAt.HasValue &&
                    (DateTime.UtcNow - prop.AlarmTriggeredAt.Value).TotalMinutes < 60;
                string lastTrigger = recentTrigger
                    ? $" ~r~| Last triggered: {prop.AlarmTriggeredAt.Value:HH:mm}"
                    : "";

                player.SendChatMessage($"~w~#{prop.Id} {prop.Name} ~b~[{prop.AlarmBrand}] {status}{lastTrigger}");
            }
        }

        // -------------------------------------------------------
        // /recentalarm — shows last triggered alarms
        // -------------------------------------------------------

        [Command("recentalarm")]
        public void RecentAlarmCommand(Client player)
        {
            if (!IsSecurityPersonnel(player) && !IsAdmin(player)) { player.SendChatMessage("~r~No permission."); return; }

            var recent = AlarmLog
                .OrderByDescending(e => e.Timestamp)
                .Take(10)
                .ToList();

            if (!recent.Any()) { player.SendChatMessage("~y~No alarm events recorded."); return; }

            player.SendChatMessage("~w~--- Recent Alarms ---");
            foreach (var evt in recent)
            {
                string resolvedTag = evt.Resolved ? "~g~[RESOLVED]" : "~r~[ACTIVE]";
                player.SendChatMessage($"{resolvedTag} ~w~{evt.Timestamp:HH:mm:ss} | {evt.PropertyName} ~b~[{evt.AlarmBrand}] ~w~| Suspect: ~r~{evt.IntruderId}");
            }
        }

        // -------------------------------------------------------
        // /alarm — arm/disarm/status for owner
        // -------------------------------------------------------

        [Command("alarm")]
        public void AlarmCommand(Client player, string action)
        {
            Property prop = GetPropertyByOwner(player.SocialClubName);
            if (prop == null) { player.SendChatMessage("~r~You don't own any registered property."); return; }
            if (!prop.AlarmInstalled) { player.SendChatMessage("~r~No alarm installed. Use /secinstall to install one."); return; }

            if (action.ToLower() == "status")
            {
                string s = prop.AlarmArmed ? "~g~ARMED" : "~y~STOPPED";
                player.SendChatMessage($"~w~{prop.Name} [{prop.AlarmBrand}]: {s}");
            }
            else
            {
                player.SendChatMessage("~r~Usage: /alarm [status] | Use /stopalarm to stop a triggered alarm.");
            }
        }

        // /stopalarm — stops the alarm of the property the player is currently standing at
        [Command("stopalarm")]
        public void StopAlarmCommand(Client player)
        {
            // Find nearest property within range that has a triggered alarm
            Property nearby = Properties.Values
                .Where(p => p.AlarmInstalled && p.AlarmTriggeredAt.HasValue &&
                            player.Position.DistanceTo(p.Position) <= p.DetectionRadius + 5f)
                .OrderBy(p => player.Position.DistanceTo(p.Position))
                .FirstOrDefault();

            if (nearby == null)
            {
                player.SendChatMessage("~r~No triggered alarm nearby. Move closer to the property.");
                return;
            }

            // Only the owner can stop it
            if (nearby.OwnerId != player.SocialClubName && !IsAdmin(player))
            {
                player.SendChatMessage("~r~You are not the owner of this property.");
                return;
            }

            nearby.AlarmArmed = false;
            nearby.AlarmTriggeredAt = null;

            var active = AlarmLog.Where(e => e.PropertyId == nearby.Id && !e.Resolved).ToList();
            foreach (var evt in active) evt.Resolved = true;

            player.SendChatMessage($"~y~[STOPPED] Alarm stopped on {nearby.Name}. Security has been notified.");
            NotifySecurityTeam($"~y~[ALARM STOPPED] {nearby.Name} — stopped by owner {player.SocialClubName}. No response needed.");
        }

        // -------------------------------------------------------

        // -------------------------------------------------------
        // Proximity loop
        // -------------------------------------------------------

        private void ProximityCheckLoop()
        {
            while (true)
            {
                System.Threading.Thread.Sleep((int)CHECK_INTERVAL_MS);
                try
                {
                    NAPI.Task.Run(() =>
                    {
                        foreach (Property prop in Properties.Values)
                        {
                            if (!prop.AlarmInstalled || !prop.AlarmArmed) continue;

                            foreach (Client player in NAPI.Pools.GetAllPlayers())
                            {
                                if (player.SocialClubName == prop.OwnerId) continue;
                                if (IsSecurityPersonnel(player)) continue;

                                float dist = player.Position.DistanceTo(prop.Position);
                                string key = $"{player.SocialClubName}_{prop.Id}";

                                if (dist <= prop.DetectionRadius)
                                {
                                    if (!PlayersInsideProperties.Contains(key))
                                    {
                                        PlayersInsideProperties.Add(key);
                                        TriggerAlarm(prop, player);
                                    }
                                }
                                else
                                {
                                    PlayersInsideProperties.Remove(key);
                                }
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    NAPI.Util.ConsoleOutput($"[AlarmSystem] Loop error: {ex.Message}");
                }
            }
        }

        private void TriggerAlarm(Property prop, Client intruder)
        {
            if (prop.AlarmTriggeredAt.HasValue &&
                (DateTime.UtcNow - prop.AlarmTriggeredAt.Value).TotalSeconds < prop.CooldownSeconds)
                return;

            prop.AlarmTriggeredAt = DateTime.UtcNow;

            AlarmLog.Add(new AlarmEvent(prop.Id, prop.Name, intruder.SocialClubName, prop.AlarmBrand));

            Client owner = GetPlayerBySocialClub(prop.OwnerId);
            owner?.SendChatMessage($"~r~[ALARM] ~w~Intruder at ~y~{prop.Name} ~w~[{prop.AlarmBrand}]! Suspect: ~r~{intruder.SocialClubName}");
            owner?.TriggerEvent("alarm:triggered", prop.Name, intruder.SocialClubName, prop.AlarmBrand);

            bool phantomBrand = prop.AlarmBrand == "PHANTOM";
            if (!phantomBrand)
                intruder.TriggerEvent("alarm:intruder_warning");

            string msg = $"~r~[ALARM] ~w~Intruder at ~y~{prop.Name} ~b~[{prop.AlarmBrand}] ~w~| Suspect: ~r~{intruder.SocialClubName} ~w~| {prop.Position.X:F0}, {prop.Position.Y:F0}";
            NotifySecurityTeam(msg);

            NAPI.Util.ConsoleOutput($"[AlarmSystem] ALARM at {prop.Name} by {intruder.SocialClubName}");
        }

        private void NotifySecurityTeam(string message)
        {
            foreach (Client player in NAPI.Pools.GetAllPlayers())
            {
                if (IsSecurityPersonnel(player) || IsAdmin(player))
                {
                    player.SendChatMessage(message);
                    player.TriggerEvent("alarm:dispatch", message);
                }
            }
        }

        private Property GetPropertyByOwner(string id) =>
            Properties.Values.FirstOrDefault(p => p.OwnerId == id);

        private Client GetPlayerBySocialClub(string id) =>
            NAPI.Pools.GetAllPlayers().FirstOrDefault(p => p.SocialClubName == id);

        private bool IsAdmin(Client player) =>
            player.HasSharedData("isAdmin") && player.GetSharedData<bool>("isAdmin");

        private bool IsSecurityPersonnel(Client player) =>
            player.HasSharedData("faction") &&
            SecurityGroups.NotifiedFactions.Contains(player.GetSharedData<string>("faction"));
    }
}
