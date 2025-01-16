# Ready Company

<div style="text-align: center;"><img height="64" src="https://github.com/Enovale/ReadyCompany/blob/master/Package/icon.png?raw=true"  alt="ReadyCompany mod icon"/></div>

A mod that adds a Ready Check system, inspired by FFXIV. It is highly customizable, and built with real users in mind.

## Screenshots

![A screenshot of the game with a Tip popup and text anchored to the hotbar that both say "1 / 1 Players are ready. MultiTap R to Unready!"](https://github.com/Enovale/ReadyCompany/blob/master/Package/Screenshots/ss_status.png?raw=true)
![A screenshot showing off the several ReadyCompany configuration options available in LethalConfig.](https://github.com/Enovale/ReadyCompany/blob/master/Package/Screenshots/ss_config.png?raw=true)

## Usage

> [!NOTE]  
> You can change how the ready and unready binds need to be pressed to activate in LethalConfig.  
> The custom string is a [Unity Interactions string](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Interactions.html).
> A more user-friendly way to configure this may be added in the future.

There is custom sound support that can be utilized by placing sound files in `BepInEx/config/Enova.ReadyCompany/CustomSounds/`.  
Your sounds must be formatted according to [LCSoundTool's wiki](https://thunderstore.io/c/lethal-company/p/no00ob/LCSoundTool/wiki/823-loading-a-sound-file-from-disk/),
and all audio files will be selected from randomly to play whenever the ready status changes.  
You can also add separate sounds for specifically when the lobby is ready by placing sound files in `BepInEx/config/Enova.ReadyCompany/CustomSounds/LobbyReady`.

## Known Issues

- Lever tooltips are sometimes inaccurate to vanilla when lobby is ready
- When the lever hasn't been pulled yet, clients can pull the lever but it will not work.
- When landing, sometimes the lever will keep the host Warning for all players.
- When starting the ship clients see "0 / 3 Ready" longer than the host does
- Not much testing has been done

## Planned (Maybe) Features

- Config option to change the binding interactions that is more user friendly
- Option to run a ready check at the Company
- Standby option: "i'm AFK but you can start without me"
- Auto ready under certain user-specified conditions

# Credits

mattymatty97 for their implementation of knowing when a player has fully connected: https://github.com/mattymatty97/LTC_LobbyControl  
LCSoundTool for the entire AudioUtility class: https://github.dev/susy-bakaa/LCSoundTool  
My friends for being nicies to me :3