# IOLipsync
Simple LipSync for the game Insult Order. The plugin plays only during the dialogue sequence before the "gameplay". 

## Download
Check [Releases](https://github.com/Rieght/IOLipsync/releases)

## How to use
Copy IOLipsync.dll to your "GameData\BepInEx\plugins" folder inside the game directory.

You can change the strength/smoothness of the mouth movement in "Gamedata\BepInEx\config\rieght.insultorder.iolipsync.cfg". (Needs to run at least once)

## Additional Informations
Lipsync during adv-events can't be added. The animation layer of the mouth movement(3) is overridden by the animation layer of the character movement(4).

The original lipsync method can be found in KutiPaku.VoiceRip().
