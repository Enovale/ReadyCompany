# Ready Company

> [!WARNING]  
> This mod is currently in a prototype stage and thus may be buggy and will not
> always look the best.

A mod that adds a Ready Check system to the game to make sure noone finds themselves
eaten by a dog when they come back from eating.

> [!NOTE]  
> You can change how the ready and unready binds need to be pressed to activate in LethalConfig.  
> The custom string is a [Unity Interactions string](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Interactions.html).
> A more user-friendly way to configure this may be added in the future.

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