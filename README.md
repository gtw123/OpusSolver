# OpusSolver

An autosolver for [Opus Magnum](http://www.zachtronics.com/opus-magnum/) puzzles.

[<img alt="example solution" width="400px" src="/images/GEN000.png" />](https://imgur.com/a/opussolver-low-cost-solutions-ZcTDolh)
[<img alt="example solution" width="400px" src="/images/GEN702.png" />](https://imgur.com/a/opussolver-low-cost-solutions-with-large-molecules-dNZW4Gw)

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

### Why does the low-cost solver fail to generate a solution for some puzzles?

The low-cost solver does not currently work for all puzzles and at the time of writing it will only successfully solve about 75% of the 24-hour challenge puzzles. If you use the "--optimize" argument then this will increase to about 95%, but it will take considerably longer to run.

## Design overview

The solutions generated by OpusSolver always consist of three main steps:
* Disassemble reagents into single atoms, one at a time
* Convert single atoms to other elements where necessary
* Assemble the products one atom at a time

Although this is a somewhat simplistic strategy and may miss some obvious optimizations, it does mean that all solutions can be constructed from a small set of discrete parts.

### The process for constructing a solution

The actual process for building a solution involves a number of steps, and these are essentially the same for both the low-cost and standard solver.

#### Step 1: Build a recipe

The first step is to generate a "recipe" for the solution. A recipe defines:
* How many of each reagent are required
* How many of each product will be built
* How many of each type of "reaction" are required
* Whether waste atoms will be generated

A "reaction" refers to a single usage of a glyph, such as using the Glyph of Calcification to convert a cardinal element to salt, or using the Glyph of Projection to promote a metal (see Recipe.cs).

For example, the puzzle GEN000 from the 24-hour challenge has two reagents:
```
  Sa     Qs--Ag    
```

It has one product:

```
   Wa            
  / \            
 Qs  Fi--Au      
```
         
An example recipe for this puzzle is:
* Has waste: True
* 2x Reagent #0: Quicksilver + Silver
* 2x Reagent #1: Salt
* 1x VanBerlo: Salt -> Fire
* 1x VanBerlo: Salt -> Water
* 1x Projection: Silver + Quicksilver -> Gold
* 1x Product #0: Water + Quicksilver + Fire + Gold

To generate a recipe, OpusSolver first determines which reactions may be needed based on which elements are present in the products but missing from the reagents (see RecipeGenerator.cs). It then constructs a system of linear equations, where the variables are the required number of each reagent, reaction and product, and there is one equation for each element (see RecipeBuilder.cs). It then uses the `lp_solve` library to try to find an exact integer solution to these equations. If it fails to find a solution, it will try again with a different scale factor (e.g. build 2, 3, 4, 5 or 6 of each product). If that still fails, it will relax some of the constraints (convert equalities to inequalities) and solve it again. In this case the recipe will generate waste atoms.

#### Step 2: Construct a solution plan

The next step is to construct a solution "plan". This consists of:
* A recipe
* The order of the elements that will be generated as each reagent is disassembled
* The order of the elements that will be consumed as each product is assembled
* Other flags that affect how the solution will be constructed

To determine the reagent element order, it first determines which type of "diassembler" will be used for each reagent. This is based on the shape and size of the reagent and will be different depending on which solver is used - see OpusSolver.Solver.LowCost.Input.MoleculeDisassemblerFactory or OpusSolver.Solver.Standard.Input.MoleculeDisassemblerFactory for examples. Similarly, to determine the product element order, it first determines which "assembler" will be used for the products.

#### Step 3: Build the element generators and element pipeline

The next step is to build a sequence of "element generators". These are abstract components that correspond to a single reagent, reaction, or product. Each element generator knows how to convert a given set of input elements to a set of output elements, and these generators are arranged in a pipeline in the same order that the reactions will occur in the final solution. e.g. Reagents first, then each glyph, then products last. See ElementPipeline.cs, ElementGenerator.cs and the classes in OpusSolver.Solver.ElementGenerators.

#### Step 4: Build the command sequence

Once the element pipeline has been built, the next step is to perform a "dry run" of the solution to determine which elements will actually be consumed and generated by each generator, and in which order. This process begins by the OutputGenerator requesting each product element one at a time. Each request is passed "up" the element pipeline until a specific element generator is able to generate it. In some cases this means the InputGenerator will generate an element directly from a reagent, while in other cases one or more elements will need to be converted by one of the intermediate generators.

The outcome of this process is a sequence of commands (see CommandSequence.cs and Command.cs). Each command applies to a specific generator and the command itself is one of the following:
* Consume: This generator will use one atom of this element
* Generate: This generator will produce one atom of this element
* PassThrough: This generator will leave this element unchanged and pass it to the next generator in the pipeline

Note that at this stage these are all abstract operations and make no assumptions about how the molecules or glyphs will be positioned in the solution, or which arms will be moving the atoms. The main reason for constructing this command sequence is so that the subsequent solving steps will have more information about how the elements need to be transformed - e.g. whether any buffering is required, which specific atoms are waste atoms, and the number of steps required when purifying metals.

#### Step 5: Create the atom generators

The next step is to create an "atom generator" for each element generator. Unlike an element generator, an atom generator is directly responsible for constructing the in-game objects (arms, tracks, glyphs etc.) that implement each step of the solution. Each generator is also responsible for programming the arms to move atoms through the generator.

Each solver has its own implementation of these atom generators - see the "Generator" classes in OpusSolver.Solver.LowCost and OpusSolver.Solver.Standard.

The atom generators for reagents and products are slightly different - they may contain one or more disassemblers or assemblers, respectively. Each of these are responsible for unbonding all the atoms from a reagent or bonding all the atoms of a product.

#### Step 6: Generate the program fragments

The commands that were generated in step 5 are now executed in sequence on the atom generator for each element generator. This results in a series of program fragments that will form the actual solution.

#### Step 7: Generate the solution

The final step is to generate the actual solution by combining together all the program fragments, removing unused parts (e.g. arms that never grab, or track that is never passed over by an arm), collating all game objects together and then saving out a solution file.

### Low-cost solver

The low-cost solver uses all the steps described above, but is designed to generate a solution with low (but not necessarily minimal) cost.

#### Solution layout

The solutions generated by this solver always contain a single main arm on a track (ArmArea.cs) which is positioned at the center of solution. Most of the atom generators just consist of a single glyph, and these are arranged in a circle around the main arm.

Each atom generator has one or more access points. These represent cells in the grid which the grabber of main arm has to move through to interact with the glyphs. The track for the main arm is constructed by taking the union of all access points and then finding a path which passes through all of them (see TrackPathBuilder.cs). There can be many different paths possible, so TrackPathBuilder uses some heuristics to try to pick a "good" one. It first tries to pick a closed (looping) path if one is available, and then chooses the path with the most number of straight segments.

After all the atom generators and the track have been created, the output generator then attempts to find locations for the products. It does this by brute-force searching for locations which are reachable by the arm on the track and don't overlap any other objects (including other products).

#### Arm movement

The atom generators used by the low-cost solver are generally pretty simple, but they make extensive use of the ArmController class. This has lower-level methods for controlling the main arm directly (e.g. grab, drop, pivot) as well as a number of higher-level operations including:
* Move the grabber to a target location/orientation
* Set a molecule to grab
* Move the molecule to a target location/orientation
* Drop the molecule at a target location/orientation

These operations in turn use the ArmPathFinder class. This uses a modified A* algorithm to find the shortest "path" to move the arm and/or molecule to a target location and orientation. Each node (or state) on the path consists of the following:
* The track cell that the arm is currently located on
* The rotation of the arm
* The location/orientation of the grabbed molecule
* Whether the arm is currently holding the molecule

The path can move from one state to the next by performing arm instructions such as move forward/backward, rotate clockwise/counterclockwise, pivot clockwise/counterclockwise or drop/grab. This allows the algorithm to find simple paths such as "move the arm from A to B" as well as more complicated paths such as "move the arm to A, drop the molecule, move the arm to B, pick up the molecule again, rotate to C" etc.

ArmPathFinder also has knowledge of what other atoms, arms and glyphs are present in the grid (see GridState.cs) and will avoid them where necessary. For example, it will avoid atom collisions while moving and rotating the arm (see RotationCollisionDetector.cs), it will avoid adding unwanted bonds or removing required bonds, and it will avoid moving atoms over glyphs if this would lead to unwanted reactions (e.g. calcification).

However, the algorithm assumes that no changes are being made to the game state other than the arm and molecule that are currently being moved. This means it is able to "look ahead" many steps and find very complicated paths, but also means that it is not currently able to do more complex tasks like bond the molecule to other atoms (except at the end of the path), or move obstacles out of the way.

#### Atom buffers

In some puzzles, the atoms generated from the reagents or other glyphs may not always be in the correct order for the product. In some cases these extra atoms can be stashed temporarily within an atom generator, but in many cases they need to be stored more permanently. Also, some puzzles may generate waste but not have the glyph of disposal available. In both of these cases, an "atom buffer" is used to store and restore the atoms.

The simplest type of atom buffer is a single arm which just stores atoms in the cells around the arm (ArmBufferNoWaste.cs). If waste is required, then an extra bonder is used to generate a waste chain (see ArmBufferWithWaste.cs). In some cases an extra unbonder may be required as well to restore atoms from the waste chain.

#### Solution parameters

A number of parameters are available which change how certain aspects of the solution are generated - e.g. which recipe to use, the order in which elements from removed/added from reagents/products, the length of the main arm, and some other potential optimizations to specific atom generators (see OpusSolver.Solver.LowCost.SolutionParameters for examples). Varying these parmeters may produce a better solution than the original, a valid solution when the original is invalid, or even an invalid solution when the original is valid. 

If OpusSolver is run with the "--optimize" option then it will perform additional analysis to determine which of these parameters are applicable to the current puzzle. It will then go through and generate a solution for every possible combination of these parameters, then validate all of them and pick the ones with lowest cost/cycles/area. This can be quick in some cases (especially when invalid solutions are generated), but can take a very long time for complex puzzles with many parameters.

### Standard solver

The standard solver is the original solver that was written for v1.0 and has remained mostly unchanged since then. Rather than generating optimal solutions, its main design goal is to be a universal solver that can solve (almost) any puzzle.

Unlike the low-cost solver, it lays out the atom generators in a strictly linear fashion, where the output arm of each generator feeds directly into the next one. It does not use any sophisticated algorithms to build the solution - each generator is pre-programmed to move atoms through in a specific way, using atom buffers where necessary. Complex reagents/products are disassembled/assembled row by row then atom by atom.

Although this solver is far from optimal, it does generally produce solutions that take less cycles than the low-cost solver.

