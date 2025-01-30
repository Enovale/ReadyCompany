# Changelog

## 1.1.1

- Fixed an issue where the player would erroniously toggle ready state while pressing 
the bind in a pause menu or in the terminal using the Hold interaction

## 1.1.0

- Fixed many bugs associated with the ship lever and it's tooltips
- MagicWesley's Galetry starts a vote just like Gordion
(along with any other custom moon that 1. Does not have time and 2. Does not spawn scrap)
- Ready status text disappears almost immediately on the client as well as the server

## 1.0.0

- Added colored checkmark/cross to indicate readiness
- Ready check disappears at the same time for clients and server
- Fixed bug where rebinding an input would cause interactions to be lost
- Show gamepad bindings when it's active
- Fixed a few bugs regarding incorrect hover tips on start match lever
- Fixed start match lever hover tips showing when they should not
- Prevent voting when special menus are open (belt bag)
- Only update and verify the ready status if you are the host player.

## 0.4.0 (1.0.RC6)

- Fixed issue where keybinds are not setup until a save is loaded
- Fixed issue where changing interaction preset through an external
mod manager like Gale would not update the interaction string
- Fixed issue where all players were assumed dead when DeadPlayersCanVote is disabled
- Fixed issue where the ship doesn't autostart when the host is dead
- Fixed a handful of misc bugs that I can't remember the specifics of.

## 0.3.1 (1.0.RC5)

- Fixed bug with Ship Lever tips not resetting
- Changed default keybind for ready/unready to 'c'
- Don't allow the player to vote under certain conditions
- Show different header text in popup when lobby is ready
- Add config option to not allow dead players to vote (Forces them to be ready)

## Version 0.3.0 (1.0.RC4)

- Ready Checking is now enabled on Gordion to leave the moon.
- LethalConfig is now an optional/soft dependency

## Version 0.2.7 (1.0.RC2)

Considered feature complete; New updates will likely slow down once 1.0 is out.

- Added presets for different interaction methods with the ready and unready binds
- The position of the ready status text is now configurable
- Added changelog to package

