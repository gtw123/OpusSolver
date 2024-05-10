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

## Building

* Install [Visual Studio 2022](https://www.visualstudio.com/downloads/).
* Ensure you have NET Framework 4.8 Development Tools intalled.
* Open OpusSolver.sln in Visual Studio.
* Build!

Currently tested on Windows only.

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
  
