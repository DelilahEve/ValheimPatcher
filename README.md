Valheim Patcher
===
### Open source Valheim modpack patcher.

Anyone may copy, modify, and/or distribute.

Made with love for the Valheim community ♥

### Configuration


###### Override modpack manifest:
Add a `config.json` file to the same folder as the executable with:
```json
{
    "manifestUrl": "https://some-manifest.url/manifest.json"
}
```
where `https://some-manifest.url/manifest.json` points to a publicly accessible 
json file.

###### Custom manifest:

The `manifest.json` consists of a few parts:

Key | Type | Purpose
--- | --- | ---
`BepInExUrl` | `string` | URL for the version of [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) compatible with your pack
`configFilesUrl` | `string` | URL pointing to the Config files you want the patcher to install
`mods` | `ModListItem[]` | The mods you want the patcher to install

###### ModListItem:

Key | Type | Purpose
--- | --- | ---
`makeFolder` | `boolean` | Whether this mod requires a folder (refer to mod install instructions)
`name` | `string` | General name to refer to this mod by (if `makeFolder` is true, this name will be used for the folder name)
`downloadUrl` | `string` | Where to find this mod (if left blank, patcher will attempt to use Nexus API*)
`nexusId` | `integer` | Nexus mod id
`zipStructure` | `string` | __Relative path__ for `.dll` files inside mod `.zip` file (leave blank if no folders)

*Some mods are only distributed via [Nexus mods](https://www.nexusmods.com/) though most are available through [Thunderstore](https://valheim.thunderstore.io/). It's recommended include both `downloadUrl` and `nexusId`, as the patcher offers the choice to prefer Nexus.

###### A minimum example of a manifest:
```json
{
    "BepInExUrl": "https://valheim.thunderstore.io/package/download/denikson/BepInExPack_Valheim/5.4.1000/",
    "configFilesUrl": "https://github.com/DelilahEve/ValheimPatcherConfig/raw/main/Darkheim/config/darkheim_config.zip",
    "mods": [
        {
            "makeFolder": true,
            "name": "ConfigurationManager",
            "downloadUrl": "https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/download/v16.3/BepInEx.ConfigurationManager_v16.3.zip",
            "nexusId": 740,
            "zipStructure": "BepInEx\\plugins"
        }
    ]
}
```