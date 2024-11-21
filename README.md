# CrimsonFAQ
`Server side only` mod 

This is a rather simple mod. It functions as an auto-responder mod.

All of it is buildable in an easy to use json file. 

You give each KeyResponse a "Key" and a "Response". Players can query for the keys and get the response as the result. 

## Installation
* Install [BepInEx](https://v-rising.thunderstore.io/package/BepInEx/BepInExPack_V_Rising/)
* Install [Bloodstone](https://thunderstore.io/c/v-rising/p/deca/Bloodstone/)
* Install [VampireCommandFramework](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/)
* Extract _CrimsonFAQ_ into _(VRising server folder)/BepInEx/plugins_
* Run server once to generate _CrimsonFAQ/responses.json_ and .cfg file
* Set your preferred prefix in the cfg file (i.e. **?**discord)

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

In the _CrimsonFAQ.cfg_ filee you will see you can change a few common things; but you'll also notice that there is a section for Hex Colors.
This will change how text is displayed in the commands & key query responses.

Here is a way to easily pick hex colors: https://g.co/kgs/DxWQHt4

## Support

Want to support my V Rising Mod development? 

Donations Accepted
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/skytech6)

Or buy/play my games! 

[Train Your Minibot](https://store.steampowered.com/app/713740/Train_Your_Minibot/) 

[Boring Movies](https://store.steampowered.com/app/1792500/Boring_Movies/)

**This mod was a paid creation. If you are looking to hire someone to make a mod for any Unity game reach out to me on Discord! (skytech6)**