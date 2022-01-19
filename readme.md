# Blowaunch
This is crossplatform open-source Minecraft launcher. \
**Curretly in development!**

## Versions
```
[+] Vanilla (Tested 1.12.2+)
[+] Fabric (Tested 1.13+)
[+] Forge (Tested 1.12.2+)
[+] Custom (Any version)
```

## Configuration
Currently we only have a console application. \
But it is not interactive at all, nor requires any arguments. \
Instead we use `config.json`, which is in the same directory as the ConsoleApp. \
If such a file won't exist, Blowaunch will create one for you. \
Here is how the config looks like (with comments):
```json5
{
  "maxRam": "",   // Maximum RAM, for example 1024M
  "jvmArgs": "",  // Java VM arguments, optional
  "gameArgs": "", // Game arguments, optional
  "username": "", // In-game username
  "customResolution": false, // Use a custom resolution
  "windowSize": {
    "X": 200.0, // Width
    "Y": 200.0  // Height
  },
  "version": "", // The minecraft version to run
  "type": 0,     // https://github.com/TheAirBlow/blowaunch/blob/main/Blowaunch.Library/Runner.cs#L52
  "forceOffline": false, // Force offline mode 
  "isDemo": false, // Minecraft Demo Mode
  "auth": { // Authentication
    "validUntil": "0001-01-01T00:00:00", // Internal (Microsoft)
    "refreshToken": "", // Internal (Microsoft)
    "type": 3,   // https://github.com/TheAirBlow/blowaunch/blob/main/Blowaunch.Library/Runner.cs#L52
    "token": "", // Your auth token (Mojang) / Internal (Microsoft)
    "uuid": "",  // Your UUID (Mojang) / Internal (Microsoft)
	"xuid": "",  // Internal (Microsoft)
  }
}
```
