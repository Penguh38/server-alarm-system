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
/secinstall [brand]        install alarm (no arg = shows brand list)
/secuninstall              remove alarm
/alarm arm|disarm|status   control your alarm
```

**Security / Admin:**
```
/alarmstatus               all properties and their current state
/recentalarm               last 10 triggered alarms
/resolvealarm [id]         mark alarm as resolved
```

**Admin only:**
```
/createproperty [name]     register property at your position
/setowner [id] [name]      assign owner by social club name
```

---

## Setup

Drop `AlarmSystem.cs` into your C# resource folder and register it. Seed properties in `OnResourceStart` or use `/createproperty` in-game.

Change which factions get notified in `SecurityGroups.NotifiedFactions`:

```csharp
public static readonly List<string> NotifiedFactions = new List<string>
{
    "LSSD", "LSPD", "ProTech Security"
};
```

---

## Dashboard

Standalone web panel for dispatchers — shows active alarms, property list, online units, and a terminal where you can test the commands live.

→ https://your-deployment.vercel.app

---

## License

Copyright (c) 2025 Penguh38. All rights reserved. Source is not for redistribution.
