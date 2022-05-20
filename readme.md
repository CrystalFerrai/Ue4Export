Command line program to export assets from a UE4 game to Json files. The main purpose for this program is to automate repetitive asset exporting when data mining games that update regularly.

## Installation

Releases can be found [here](https://github.com/CrystalFerrai/Ue4Export/releases).

This program is released standalone, meaning there is no installer. Simply extract the files to a directory to install it.

## Usage

You will first need to locate the paths to the specific assets you want to export. You can use a program like Unrealpak or Fmodel to help locate the assets. Create a text file with a list of all of the asset paths you want to export. For example:

**assetlist.txt**
```
MyGame/Content/Maps/Map1/Map1
MyGame/Content/BP/SomeSystem/SomeBP
```

Next, create a batch script to run the program, placing it next to Ue4Export.exe. Here is an example:

**RunUe4Export.bat**
```
@Ue4Export "C:\Games\MyGame\Content/Paks" "assetlist.txt" "C:\Output"
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