# OpusSolver

An autosolver for [Opus Magnum](http://www.zachtronics.com/opus-magnum/) puzzles. It generates correct but very inefficient solutions.

This version is a command-line program which generates `.solution` files from `.puzzle` files. If you're looking for the original version which works on the actual game screen, see https://github.com/gtw123/OpusSolver/tree/v1.0

## What can it solve?

* All puzzles in the main campaign
* All journal puzzles (except production puzzles)
* Most puzzles created in the standard editor

## What can't it solve?

* Production puzzles
* Puzzles which have molecules that can't be reduced to single atoms (e.g. if the Glyph of Unbonding isn’t allowed).
* Puzzles using features not in the standard editor, like triplex bonds between non-fire atoms, partial triplex bonds, or disconnected atoms.
* Puzzles which disallow pistons or tracks.
 
## Building

These steps have currently been tested on Windows only.

### Building OpusSolver

* Install [Visual Studio 2022](https://www.visualstudio.com/downloads/).
* Ensure you have NET Framework 4.8 Development Tools intalled.
* Open OpusSolver.sln in Visual Studio.
* Build!

### Building libverify
Libverify from omsim is required in order to verify solutions.

To build libverify on Windows:
* Clone the omsim repo from https://github.com/ianh/omsim
* Install MSYS2 by following the steps on https://www.msys2.org/ to download and run the installer.
* Once installed it will automatically launch the MSYS2 UCRT64 environment.
* Install the required dev tools: `pacman -S mingw-w64-ucrt-x86_64-toolchain make`
* `cd <path to your omsim repo>` (note that the `C:` drive is located at `/C` in the MSYS2 console).
*	`make libverify.dll`
  * If this fails to build, make sure you are using the UCRT64 version of the MSYS2 environment. Launch MSYS2 UCRT64 from the start menu, or run `C:\msys64\ucrt64.exe`.
*	Copy the built `libverify.dll` into `OpusSolver\bin\Debug` and `OpusSolver\bin\Release`.

To build libverify on other platforms, follow the steps in the README in the omsim repo.

## Usage

* To generate a solution for a puzzle, simply run `OpusSolver.exe <path to the .puzzle file>`. This will create a corresponding `.solution` file in the current directory.
* You can also give it multiple files or directories to run on.
* For other options, run `OpusSolver.exe` with no arguments.

## FAQ

### Where do I get `.puzzle` files from?

Puzzles created in the game are located at `C:\Users\<username>\Documents\My Games\Opus Magnum\<steam ID>\custom`

Puzzles downloaded from the Steam workshop are at `C:\Users\<username>\Documents\My Games\Opus Magnum\<steam ID>\workshop`

For other examples, including `.puzzle` files for the built-in puzzles, see [omsim](https://github.com/ianh/omsim/tree/master/test).

### How do I run a generated `.solution` file in the game?

* Copy the `.solution` file directly into `C:\Users\<username>\Documents\My Games\Opus Magnum\<steam ID>` then restart Opus Magnum.
* Go to the corresponding puzzle within the game.
* The generated solution will be called `Generated solution`.
  
