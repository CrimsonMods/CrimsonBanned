# CrimsonBanned
`Server side only` mod 

This mod allows for specified lengths of bans for the following systems:

* Chat - players will not be able to type in chat
* Voice - players will not be able to use the voice chat system (speak or hear)
* Server - bans the player from the server

All of these options are by lengths of time either in minutes, hours, or days. As well supplying a 0 for any length will result in perma ban.

# Notes
* Voice Bans are a tad finicky. They may apply immediately or they may apply the next time they log in to the game.
* Server Bans expiring are on a 1 minute loop, so upon a ban ending they may need to wait up to a minute before they can connect again.

## Installation
* Install [BepInEx](https://v-rising.thunderstore.io/package/BepInEx/BepInExPack_V_Rising/)
* Install [VampireCommandFramework](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/)
* Extract _CrimsonBanned_ into _(VRising server folder)/BepInEx/plugins_
* Run server once to generate the .cfg file
* Setup your .cfg file

## Optional Dependencies

If you want to sync your bans across multiple servers
* Install [CrimsonSQL](https://thunderstore.io/c/v-rising/p/skytech6/CrimsonSQL/)

If you want to keep a record of bans
* Install [CrimsonLog](https://thunderstore.io/c/v-rising/p/skytech6/CrimsonLog/)

## Config
```
## If this is set to true, the player will never be notified when chat or voice banned.
# Setting type: Boolean
# Default value: true
ShadowBan = true
```
If you have ShadowBan set to true it will not display any messages to the players banned or notify them when their ban ends. 

```
## The path from root to the banlist.txt file
# Setting type: String
# Default value: save-data/Settings/banlist.txt
BanFilePath = save-data/Settings/banlist.txt
```
The default setting here for where your banlist.txt file is located should be correct. But if not, please do adjust it.

## Optional Configs for CrimsonSQL
This config options will only appear if you have CrimsonSQL installed.
```
## If this is set to true, the plugin will use CrimsonSQL to store bans.
# Setting type: Boolean
# Default value: false
UseSQL = true
```
If you want your bans synced across multiple servers via SQL set this to true.

```
## The interval in minutes to sync the database.
# Setting type: Int32
# Default value: 60
SyncInterval = 60
```
How often in minutes do you want the server to sync with the SQL database. This will retrieve bans that other servers have issued.

## messages.json
CrimsonBanned supports customizing how your command outputs appear such as when using '.banned list (type)' or '.banned check (player)`.

```json
[
  {
    "Key": "CheckHeader",
    "Value": "\n{player}\u0027s ({id}) Bans:"
  },
  {
    "Key": "CheckBanLine",
    "Value": "\n{type} Ban\nIssued: {issued}\nRemaining: {remaining}\nReason: {reason}"
  },
  {
    "Key": "ListBan",
    "Value": "\n{player} ({id}) - {remaining}"
  }
]
```

CheckHeader is the base player information that will be displayed when you call for a check on a player.

CheckBanLine is generated for each active ban that player has. 

ListBan is generated for each ban of that type.

Valid Parameters:
* {player} = The player's character name if known, otherwise _Unknown_
* {id} = The player's SteamID
* {issued} = The date and time the ban was issued
* {reason} = The reason given for the ban if provided
* {by} = The character name of the admin who issued the ban
* {type} = The type of ban; i.e. server, chat, voice
* {until} = The date and time the ban will expire
* {remainder} = The length of time remaining in the ban
* {local} = If the ban was issued from this server (Only applicable if using CrimsonSQL)

As well you can use any valid [Rich Text](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichText.html) such as 
```
<color=#00000>{player}</color>
```

> [!TIP]
> You can visually edit this JSON with [JSON Rising](https://thunderstore.io/c/v-rising/p/skytech6/JSONRising/).

## Commands
CrimsonBanned has a lot of Commands, please refer to the [Wiki](https://thunderstore.io/c/v-rising/p/skytech6/CrimsonBanned/wiki/) for each command group.

## Support

Want to support my V Rising Mod development? 

Donations Accepted
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/skytech6)

Or buy/play my games! 

[Train Your Minibot](https://store.steampowered.com/app/713740/Train_Your_Minibot/) 

[Boring Movies](https://store.steampowered.com/app/1792500/Boring_Movies/) **Free to Play!**

**This mod was a paid creation. If you are looking to hire someone to make a mod for any Unity game reach out to me on Discord! (skytech6)**