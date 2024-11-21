# CrimsonBanned
`Server side only` mod 

This mod allows for specified lengths of bans for the following systems:

* Chat - players will not be able to type in chat
* Voice - players will not be able to use the voice chat system (speak or hear)
* Server - bans the player from the server

All of these options are by lengths of time either in minutes, hours, or days.

Use the ServerConnection system to link CrimsonBanned to a MySQL database and sync your bans between servers.

## Installation
* Install [BepInEx](https://v-rising.thunderstore.io/package/BepInEx/BepInExPack_V_Rising/)
* Install [VampireCommandFramework](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/)
* Extract _CrimsonBanned_ into _(VRising server folder)/BepInEx/plugins_
* Run server once to generate the .cfg file
* Setup your .cfg file

```json 
[
  {
    "Key": "discord",  // The key players input
    "Response": "Join our discord at discord.gg/RBPesMj",  // what the server responds with
    "Description": "discord link",  // a short description of what this displays; used for the .faq list command
    "IsGlobal": true,  // global will broadcast to everyone, false it will be private only
    "PermissionLevel": 0,  // controls who can access this key. 0 = Everyone, 1 = Trusted, 2 = Admin
    "GlobalCooldownSeconds": 30  // how often in seconds should it be broadcast global? If it is spammed, subsequential requests will be displayed only to requester
  }
]
```

## Support

Want to support my V Rising Mod development? 

Donations Accepted
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/skytech6)

Or buy/play my games! 

[Train Your Minibot](https://store.steampowered.com/app/713740/Train_Your_Minibot/) 

[Boring Movies](https://store.steampowered.com/app/1792500/Boring_Movies/) **Free to Play!**

**This mod was a paid creation. If you are looking to hire someone to make a mod for any Unity game reach out to me on Discord! (skytech6)**