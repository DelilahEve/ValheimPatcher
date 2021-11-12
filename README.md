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
    "manifestOptions": [
        {
            "name": "Custom",
            "manifestUrl": "https://some-manifest.url/manifest.json"
        },
        ...
    ]
}
```
where `https://some-manifest.url/manifest.json` points to a publicly accessible 
json file.

###### Custom manifest:

The `manifest.json` consists of a few parts:

Key | Type | Purpose
--- | --- | ---
`configFilesUrl` | `string` | URL pointing to the Config files you want the patcher to install
`mods` | `ModListItem[]` | The mods you want the patcher to install

###### ModListItem:

Key | Type | Purpose
--- | --- | ---
`name` | `string` | Plugin name
`package` | `string` | Package (namespace)
`downloadUrl` | `string` | Where to find this mod (Patcher will auto-resolve this when name/package are provided)

###### A minimum example of a manifest:
```json
{
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
