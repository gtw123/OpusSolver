# OpusSolver

An automated solver (bot) for [Opus Magnum](http://www.zachtronics.com/opus-magnum/) puzzles.

See it in action: https://www.youtube.com/watch?v=egrs04Ko864

Example solutions: https://imgur.com/a/DBzN0wi

## What can it solve?

* All puzzles in the main campaign
* All journal puzzles (except production puzzles)
* Most workshop puzzles

## What can't it solve?

* Production puzzles
* Puzzles which have molecules that can't be reduced to single atoms (e.g. if the Glyph of Unbonding isn’t allowed).
* Puzzles using features not in the standard editor, like triplex bonds between non-fire atoms, partial triplex bonds, or disconnected atoms.
* Very large puzzles. (In *theory* it can solve arbitrarily large puzzles, but it has problems as the game starts to slow down a lot when there are many instructions.)

## Building

* Install [Visual Studio 2017](https://www.visualstudio.com/downloads/).
* Ensure you have [NET Framework 4.7 Development Tools](https://stackoverflow.com/questions/43316307/cant-choose-net-4-7]) installed.
* Open Opus.sln in Visual Studio.
* Build!

Currently supports Windows only.

## Using

* Run OpusSolver.
* Run Opus Magnum.
* Select a puzzle and make a new, empty solution.
* Press win-shift-A to start solving the puzzle.
* Don't touch the mouse or keyboard while it's working or it might get confused.
* Press escape if you want to abort.

## Known Issues

* Doesn't work if you're running Opus Magnum full screen in a different resolution to your display. Make sure one game pixel equals one display pixel.
* Doesn't work if you start with a non-empty solution. Even if you ctrl-A and delete, it may still not work because the instruction grid may not have scrolled back to the origin. It's best to just start with a new solution each time.
* May get confused if the Steam overlay comes up while it's generating a solution.
* Has trouble rendering program instructions for very large puzzles. This is because the game slows down as you add more and more instructions, especially when you have a few thousand. The solver does try to slow down the rendering to compensate for this but eventually it will get out of sync with what's on the screen and will fail.
* For repeating molecules it "cheats" by only generating the first 6 copies of the molecule, which is enough to solve a puzzle.

**How does it work?**

It starts off by analyzing the game window to see what glyphs/mechanisms are available, and what the reagents/products are. To analyze atoms it drags each molecule onto the hex grid so that it’s the correct size, then matches the center of each atom against a set of reference images. Because of the specular lighting, it can’t do an exact pixel match. Instead it applies a brightness threshold to the image so that it can detect the symbol at the center of each atom.

Once it’s analyzed everything it then constructs a solution. It uses a fairly “brute-force” algorithm that involves unbonding all reagents to single atoms, then transporting them through a linear “pipeline” to convert them to other elements. Each component of the pipeline can do one type of conversion (e.g. cardinal to salt, or two salt to mors/vitae), or can pass an atom through unchanged. Finally, each atom is supplied to an assembly area which builds a product one row at a time. Some optimization is also applied to avoid creating redundant arms, tracks or glyphs.

Finally, it renders the solution by dragging the reagents/products/glyphs/mechanisms onto the grid and then writing all the program instructions. The game slows down a lot when you have many instructions, which means the renderer starts missing instructions. To compensate it checks each row after it’s rendered and if it finds an error it will slow down slightly and try again. For a really large program it will still eventually get out of sync and fail though.
