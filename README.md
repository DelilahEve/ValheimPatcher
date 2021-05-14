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
`BepInEx` | `BepInExMeta` | package and name for [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
`configFilesUrl` | `string` | URL pointing to the Config files you want the patcher to install
`mods` | `ModListItem[]` | The mods you want the patcher to install

###### BepInExMeta:

Key | Type | Purpose
--- | --- | ---
`name` | `string` | Plugin name
`package` | `string` | Package (namespace)

###### ModListItem:

Key | Type | Purpose
--- | --- | ---
`makeFolder` | `boolean` | Whether this mod requires a folder (refer to mod install instructions)
`name` | `string` | Plugin name (if `makeFolder` is true, this name will be used for the folder name)
`package` | `string` | Package (namespace)
`downloadUrl` | `string` | Where to find this mod (if left blank and there's no package/name, patcher will ignore)
`zipStructure` | `string` | __Relative path__ for `.dll` files inside mod `.zip` file (leave blank if no folders)

###### A minimum example of a manifest:
```json
{
    "BepInEx": {
        "package": "denikson",
        "name": "BepInExPack_Valheim"
    },
    "configFilesUrl": "https://github.com/DelilahEve/ValheimPatcherConfig/raw/main/Darkheim/config/darkheim_config.zip",
    "mods": [
        {
            "makeFolder": true,
            "name": "ConfigurationManager",
            "package": "",
            "downloadUrl": "https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/download/v16.3/BepInEx.ConfigurationManager_v16.3.zip",
            "zipStructure": "BepInEx\\plugins"
        }
    ]
}
```
