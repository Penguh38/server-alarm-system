# server-alarm-system

Property alarm system for RAGE:MP roleplay servers. Owners install an alarm brand on their property — if someone who isn't the owner walks in, the owner and all online security/law enforcement units get notified instantly.

Built this because most RP servers handle break-ins through /me's and trust. This automates it.

---

## How it works

Every 2 seconds the server checks if any player is inside a registered property zone. If the property has an armed alarm and the player isn't the owner or a security unit, it triggers — notifies the owner if they're online, broadcasts to all faction members, and logs the event.

Phantom tier is silent — the intruder gets no warning that the alarm went off.

---

## Alarm brands

Six tiers with different detection radius, cooldown, and monthly price:

| Brand | Tier | Price/mo | Radius | Cooldown |
|---|---|---|---|---|
| SENTINEL | Basic | $500 | 8m | 120s |
| GUARDIAN | Standard | $1,200 | 12m | 90s |
| VIPER | Advanced | $2,500 | 15m | 60s |
| NEXUS | Premium | $4,500 | 18m | 45s |
| FORTRESS | Elite | $8,000 | 22m | 30s |
| PHANTOM | Black | $15,000 | 28m | 15s |

---

## Commands

**Owner:**
```
/secinstall [brand]    install alarm on your property (no arg = shows all brands with prices)
/secuninstall          remove alarm from your property
/stopalarm             stop a triggered alarm — use this when it's a false alarm or you're at the door yourself
/alarm status          check current alarm state
```

**Security / Admin:**
```
/alarmstatus           shows all properties and their current alarm state
/recentalarm           last 10 triggered alarms with suspect names and timestamps
```

**Admin only:**
```
/createproperty [name]    register a new property at your current position
/setowner [id] [name]     assign owner to a property by social club name
```

---

## Setup

Drop `AlarmSystem.cs` into your C# resource folder and register it. Properties can be seeded in `OnResourceStart` or added in-game via `/createproperty`.

Change which factions get notified in `SecurityGroups.NotifiedFactions`:

```csharp
public static readonly List<string> NotifiedFactions = new List<string>
{
    "LSSD", "LSPD", "ProTech Security"
};
```

---

## Dashboard

Standalone web dispatch panel showing live alarm events, property statuses, and online units. Includes an interactive terminal where you can test all commands and see how they behave.

→ https://server-alarm-system.vercel.app

---

## License

Copyright (c) 2025 Penguh38. All rights reserved. Source is not for redistribution.
