Command line program to export assets from a UE4 game to Json files. The main purpose for this program is to automate repetitive asset exporting when data mining games that update regularly.

## Installation

Releases can be found [here](https://github.com/CrystalFerrai/Ue4Export/releases).

This program is released standalone, meaning there is no installer. Simply extract the files to a directory to install it.

You will need to install the [.NET 6.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) if you do not already have it.

## Usage

You will first need to locate the paths to the specific assets you want to export. You can use a program like Unrealpak or Fmodel to help locate the assets.

### Asset List

Create a text file with a list of all of the asset paths you want to export. Here is a simple example.

**assetlist.txt**
```
MyGame/Content/Maps/Map1/Map1
MyGame/Content/BP/SomeSystem/SomeBP
```

By default, assets are exported as text. You can choose to export assets in their native format instead by placing a `[Raw]` header above a list of assets. You can switch back to text using a `[Text]` header. Using `[Texture]` will export all texture from an asset. You can also combine multiple export types by comma separating them, such as `[Raw,Texture]`.

Lines that start with `#` are comments which are ignored by the program.

Asset paths in the asset list file also have basic wildcard support. A `?` can represent any single character in the path and a `*` can represent 0 or more characters.

Here is a more complex asset list example.

**assetlist.txt**
```
[Raw]
# Export all assets within a directory (including subdirectories)
MyGame/Content/Maps/Map1/*

[Text]
MyGame/Content/BP/SomeSystem/SomeBP.uasset

[Texture]
# Export all assets in a directory as textures
MyGame/Content/UI/Icons/*

# Export the following assets as both Raw and Json
[Raw,Text]
MyGame/Content/UI/Component/MyButton
```

Note: When specifying an asset path, you must include a file extension unless the extension is `uasset` or `umap`, in which case it is optional.

### Batch Script

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