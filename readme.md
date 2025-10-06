Command line program to export assets from a UE4 or UE5 game to Json files. The main purpose for this program is to automate repetitive asset exporting when data mining games that update regularly.

## Installation

Releases can be found [here](https://github.com/CrystalFerrai/Ue4Export/releases).

This program is released standalone, meaning there is no installer. Simply extract the files to a directory to install it.

You will need to install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) if you do not already have it.

## Usage

You will first need to locate the paths to the specific assets you want to export. You can use a program like Unrealpak or Fmodel to help locate the assets.

### Asset List

Create a text file with a list of all of the asset paths you want to export. Here is a simple example.

**assetlist.txt**
```
MyGame/Content/Maps/Map1/Map1
MyGame/Content/BP/SomeSystem/SomeBP
```

By default, the program tries to determine the best export format for each asset. You can specify specific export formats to override this behavior by placing a header above a group of asset paths.

* `[Auto]` Attempts to determine the best format to use for each asset. Falls back on `[Raw]` if unable to find a format.
* `[Raw]` Exports assets in their native format.
* `[Text]` Attempts to export assets as plain text.
* `[Texture]` Attempts to extract texture data from assets and exports that data as images.

You can also combine multiple export types by comma separating them, such as `[Raw,Texture]`. `[Auto]` cannot be combined with other formats.

Lines that start with `#` are comments which are ignored by the program.

Asset paths in the asset list file also have basic wildcard support. A `?` can represent any single character in the path and a `*` can represent 0 or more characters.

Here is a more complex asset list example.

**assetlist.txt**
```
[Raw]
# Export all assets within a directory (including subdirectories)
MyGame/Content/Maps/Map1/*

[Text]
# Export a specific asset
MyGame/Content/BP/SomeSystem/SomeBP.uasset

[Texture]
# Export all assets in a directory as textures
MyGame/Content/UI/Icons/*

# Export the following assets as both Raw and Json
[Raw,Text]
MyGame/Content/UI/Component/MyButton
MyGame/Content/UI/Screen/MyStartScreen
```

Note: When specifying an asset path, you must include a file extension unless the extension is `uasset` or `umap`, in which case it is optional.

### Engine Version

You will need to locate the version of Unreal Engine that the game uses so you can pass it on the command line. Following is a list of all supported versions. If there is a game-specific version that matches the game you are working with, be sure to pass in that game specific version. Otherwise, pass in the version of the engine the game was built with.

```
UE4_0
UE4_1
UE4_2
UE4_3
UE4_4
UE4_5
ArkSurvivalEvolved
UE4_6
UE4_7
UE4_8
UE4_9
UE4_10
SeaOfThieves
UE4_11
GearsOfWar4
UE4_12
UE4_13
StateOfDecay2
UE4_14
TEKKEN7
UE4_15
UE4_16
PlayerUnknownsBattlegrounds
TrainSimWorld2020
UE4_17
AWayOut
UE4_18
KingdomHearts3
FinalFantasy7Remake
AceCombat7
FridayThe13th
GameForPeace
UE4_19
Paragon
UE4_20
Borderlands3
UE4_21
StarWarsJediFallenOrder
Undawn
UE4_22
UE4_23
ApexLegendsMobile
UE4_24
UE4_25
UE4_25_Plus
RogueCompany
DeadIsland2
KenaBridgeofSpirits
CalabiYau
SYNCED
OperationApocalypse
Farlight84
UE4_26
GTATheTrilogyDefinitiveEdition
ReadyOrNot
BladeAndSoul
TowerOfFantasy
Dauntless
TheDivisionResurgence
StarWarsJediSurvivor
Snowbreak
TorchlightInfinite
QQ
WutheringWaves
DreamStar
MidnightSuns
UE4_27
Splitgate
HYENAS
HogwartsLegacy
OutlastTrials
Valorant
Gollum
Grounded
SeekersofSkyveil
MortalKombat1
UE4_LATEST
UE5_0
MeetYourMaker
UE5_1
3on3FreeStyleRebound
UE5_2
DeadByDaylight
PaxDei
TheFirstDescendent
UE5_3
MarvelRivals
WildAssault
UE5_4
FunkoFusion
InfinityNikki
NevernessToEverness_CBT1
Gothic1Remake
SplitFiction
WildAssault
InZOI
TempestRising
MindsEye
DeadByDaylight
Grounded2
MafiaTheOldCountry
UE5_5
Brickadia
Splitgate2
DeadzoneRogue
MotoGP25
Wildgate
ARKSurvivalAscended
NevernessToEverness
FateTrigger
UE5_LATEST
UE5_7
```

To locate the engine version a game was built with, check the properties of the game's exe file. The version listed will be the Unreal Engine version.

### Batch Script

Next, create a batch script to run the program, placing it next to Ue4Export.exe. Here is an example:

**RunUe4Export.bat**
```
@Ue4Export "C:\Games\MyGame\Content\Paks" UE4_27 "assetlist.txt" "C:\Output"
```

The first parameter is the directory containing the pak files for your game. The second is the engine version. The third is the list of assets you created for exporting. The fourth is a directory in which to output the Json files for the assets.

Replace the parameters in your script with appropriate values for our use case. Then run your batch script and wait for it to finish. If there were no errors, you should find the output files in the specified output directory.

If the pak files you are exporting from are encrypted, you will need to pass the optional parameter `--key [value]` where `[value]` is the AES key as a hexadecimal string.

For Unreal 5 games, you will also need to specify a mappings file using the optional parameter `--mappings [path]` where `[path]` is the path to a usmap file for the game. There are a few programs that can assist with generating a usmap file. The one that I personally use is [Dumper-7](https://github.com/Encryqed/Dumper-7) which is simple and effective for the games I have tested. You will need a DLL injector to inject Dumper-7. Any injector will work, but if you are looking for a simple one, you can use [DllInjector](https://github.com/CrystalFerrai/DllInjector). Warning: If the game you are injecting into has anti-cheat software, the injection may be detected as a cheat.

### Command Line Parameter Reference

Run the program in a console window with no parameters to see a list of options like the following.

```
Usage: Ue4Export [[options]] [game assets directory] [asset list file] [output directory]

  [game assets directory]  Path to a directory containing game assets.

  [engine version]         The engine version the game was built with. See list below for values.

  [asset list file]        Path to text file with list of asset paths to export. See readme.md
                           for more details.

  [output directory]       Directory to output exported assets.

Options

  --mix-output       Do not clear the contents of the output directory before exporting.

  --skip-existing    Skip exporting assets where a matching output file already exists.
                     Implies --mix-output

  --mappings [path]  The path to a usmap file for the game. This is necessary if the game contains
                     unversioned data, such as a UE5 game. See readme for more information.

  --key [key]        The AES encryption key for the game's data if the data is encrypted.

  --no-subdirs       Do not search subdirectories for game assets. Only search the top level
                     game assets directory.

  --quiet            Minimal logging. Skips listing individual assets.

  --silent           No logging unless an error occurs.

Game engine versions
  Pass in the engine version that best matches the game being dumped. If the game has a
  specialized version, pass that in. Otherwise, pass in the engine version the game was
  built with, which can be found in the properties of the game's exe.

  (Will also print a full list of supported engine versions here.)
```

## Troubleshooting
If you are running into issues where assets cannot be found or are not being exported properly, it is usually due to one of the following issues.

1. The path to the game asset directory is incorrect. In most cases, this should be the directory which contains the game's .pak files or a parent of that directory.
2. You supplied the wrong engine version on the command line. Double check that it is correct.
3. The game's data is encrypted. You will need to obtain the decryption key and supply it on the command line.
4. The game requires a mappings file, and you have either not supplied one, supplied an incorrect one (such as from a different game), or supplied one for a different version of the game. All UE5 games require a mappings file, and in rare cases UE4 games also need them. Verify that you are supplying a valid one, if needed.
5. The version of CUE4Parse being used by this tool does not support the game you are attempting to export from. There is not much you can do about this aside from ask for an update or fork the repo and do your own update.

If you have verified everything above, but something is still not working, you can [open an issue](https://github.com/CrystalFerrai/Ue4Export/issues). Note that issues are not checked regularly, so there may be a long delay before you receive any response. 

## Building
Clone the repository, including submodules.
```
git clone --recursive https://github.com/CrystalFerrai/Ue4Export.git
```

You can then open and build Ue4Export.sln.

To publish a build for release, run this command from the directory containing the SLN.
```
dotnet publish -p:DebugType=None -r win-x64 -c Release --self-contained false
```

The resulting build can be located at `Ue4Export\bin\Release\net8.0\win-x64\publish`.