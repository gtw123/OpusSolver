# OpusSolver

An autosolver for [Opus Magnum](http://www.zachtronics.com/opus-magnum/) puzzles.

This version is a command-line program which generates `.solution` files from `.puzzle` files. If you're looking for the original version which works on the actual game screen, see https://github.com/gtw123/OpusSolver/tree/v1.0

## What can it solve?

OpusSolver currently contains two solvers: `standard` and `lowcost`. The `standard` solver can solve:
* All puzzles in the main campaign
* All journal puzzles (except production puzzles)
* Most puzzles created in the standard editor

However, it doesn't produce very optimal solutions.

The `lowcost` solver generates solutions which are more optimized for cost, but it's more restricted in the types of puzzles it can solve. It was designed to solve puzzles from the [Opus Magnum 24-Hour Challenge](https://www.reddit.com/r/opus_magnum/comments/1chs6eu/opus_magnum_24hour_challenge/) and therefore cannot solve puzzles with repeating molecules, puzzles with multiple large reagents, or a number of other cases. 

## What can't it solve?

* Production puzzles
* Puzzles which have molecules that can't be reduced to single atoms (e.g. if the Glyph of Unbonding isn’t allowed).
* Puzzles using features not in the standard editor, like triplex bonds between non-fire atoms, partial triplex bonds, or disconnected atoms.
* Puzzles which disallow pistons or tracks.
 
## Building

### Building OpusSolver on Windows

* Install [Visual Studio 2022](https://www.visualstudio.com/downloads/).
* Ensure you have NET 8.0 Development Tools installed.
* Open OpusSolver.sln in Visual Studio.
* Build!

### Building on other platforms

Only Windows has currently been tested, but it should be possible to build it on other platforms using the .NET 8.0 SDK.

Some native libraries are required:
* `Libverify` from `omsim`. Windows and Linux binaries can be downloaded from https://github.com/ianh/omsim/releases.
* `lp_solve` can be downloaded from https://sourceforge.net/projects/lpsolve/files/lpsolve/5.5.2.11/. The "dev" version is required, e.g. `lp_solve_5.5.2.11_dev_win64.zip`

## Usage

### Generating solutions

To generate a solution for a puzzle, simply run `OpusSolver.exe <path to the .puzzle file>`. This will create a corresponding `.solution` file in the current directory. You can also give it multiple files or directories to run on. To see a list of all options, run `OpusSolver.exe` with no arguments.

By default it will only attempt to generate one solution per puzzle. This is fast, but may fail for some puzzles, especially when using the `lowcost` solver. If you use the `--optimize` argument then it will generate multiple solutions for each puzzle and pick the best ones. This will **significantly** increase runtime but is more likely to generate a valid solution.

A PowerShell wrapper script is also included for convenience, which can be run using `run.bat` or `run.ps1`. This script is set up to generate solutions for Opus Magnum 24-Hour Challenge puzzles. If you want to generate optimized low-cost solutions for all 1000 of the 24-hour challenge puzzles, be sure to use both the `--optimize` and `--parallel` options when running this script. However, be aware that it may take over an hour to generate solutions for all of them.

### Analyzing puzzles

Instead of generating soluions, the `--analyze` option will generate a list of all puzzles, their reagents and products, and the reactions required to generate	the products. 

## FAQ

### Where do I get `.puzzle` files from?

Puzzles created in the game are located at `C:\Users\<username>\Documents\My Games\Opus Magnum\<steam ID>\custom`

Puzzles downloaded from the Steam workshop are at `C:\Users\<username>\Documents\My Games\Opus Magnum\<steam ID>\workshop`

For other examples, including `.puzzle` files for the built-in puzzles, see [omsim](https://github.com/ianh/omsim/tree/master/test).

### How do I run a generated `.solution` file in the game?

* Copy the `.solution` file directly into `C:\Users\<username>\Documents\My Games\Opus Magnum\<steam ID>`.
* If Opus Magnum is already running, you can press F10 to force it to reload the solution files. However, this only works when you already have a solution open in the game (it does not work on the puzzle menu screen). Also, if you try to open a solution before pressing F10, the game may overwrite the file on disk with the version already loaded in the game. It's recommend that you either:
  1. Open the existing solution in the game first, then copy across the new .solution file, then press F10 to reload it, or
  2. Copy the .solution file first, then open a **different** solution in the game, press F10, then open the solution you copied in.

### Why does the lowcost solver fail to generate a solution for some puzzles?

The lowcost solver does not currently work for all puzzles and at the time of writing it will only successfully solve about 75% of the 24-hour challenge puzzles. If you use the "--optimize" argument then this will increase to about 95%, but it will take considerably longer to run.



