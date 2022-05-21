Command line program to export assets from a UE4 game to Json files. The main purpose for this program is to automate repetitive asset exporting when data mining games that update regularly.

## Installation

Releases can be found [here](https://github.com/CrystalFerrai/Ue4Export/releases).

This program is released standalone, meaning there is no installer. Simply extract the files to a directory to install it.

You will need to install the [.NET 6.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) if you do not already have it.

## Usage

You will first need to locate the paths to the specific assets you want to export. You can use a program like Unrealpak or Fmodel to help locate the assets. Create a text file with a list of all of the asset paths you want to export. For example:

**assetlist.txt**
```
MyGame/Content/Maps/Map1/Map1
MyGame/Content/BP/SomeSystem/SomeBP
```

By default, assets are exported as Json. You can choose to export raw assets (.uasset files) instead by placing a `[Raw]` header above a list of assets. You can switch back to Json using a `[Json]` header or export as both using `[Raw,Json]`. You can also leave comments by starting a line with `#`. Here is an example asset list showing these options.

```
[Raw]
MyGame/Content/Maps/Map1/Map1

# Some comment
[Json]
MyGame/Content/BP/SomeSystem/SomeBP

# Export these as Raw and Json
[Raw,Json]
MyGame/Content/UI/Component/MyButton
```

Next, create a batch script to run the program, placing it next to Ue4Export.exe. Here is an example:

**RunUe4Export.bat**
```
@Ue4Export "C:\Games\MyGame\Content\Paks" "assetlist.txt" "C:\Output"
```

The first parameter is the directory containing the pak files for your game. The second is the list of assets you created for exporting. The third is a directory in which to output the Json files for the assets.

Now run your batch script and wait for it to finish. If there were no errors, you should find the output files in the specified output directory.

## Building
Clone the depot, including submodules.
```
git clone --recursive https://github.com/CrystalFerrai/Ue4Export.git
```

You can then open and build Ue4Export.sln.

To publish a build for release, run this command from the directory containing the SLN.
```
dotnet publish -p:DebugType=None -r win-x64 -c Release --self-contained false
```

The resulting build can be located at `Ue4Export\bin\Release\net6.0\win-x64\publish`.