# IOLipsync
Simple LipSync for the game Insult Order. The plugin plays only during the dialogue sequence before the "gameplay". It adds mouth and eye movement while the character talks. Replaces also the original lipsync.

## Download
Check [Releases](https://github.com/Rieght/IOLipsync/releases)

## How to use
Copy IOLipsync.dll to your "GameData\BepInEx\plugins" folder inside the game directory.

You can change the strength/smoothness of the mouth/eye movement in "Gamedata\BepInEx\config\rieght.insultorder.iolipsync.cfg" (Needs to run at least once) or via the F12-menu.
Eyemovement can be disabled.

## Additional Informations
Lipsync during adv-events can't be added. The animation layer of the mouth movement(3) is overridden by the animation layer of the character movement(4).

The original lipsync method can be found in KutiPaku.VoiceRip().

## My other plugins
[IOSubtitles](https://github.com/Rieght/IOSubtitles)
