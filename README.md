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
* Puzzles which have molecules that can't be reduced to single atoms, e.g.
  * Puzzles which don't allow the Glyph of Bonding and the Glyph of Unbonding (except for trivial puzzles).
  * Puzzles that have triplex bonds in the products but don't allow the Glyph of Triplex bonding.
* Puzzles using features not in the standard editor, like triplex bonds between non-fire atoms, partial triplex bonds, or disconnected atoms.
* Very large puzzles. (In theory it can solve arbitrarily large puzzles, but it has problems as the game starts to slow down a lot when there are many instructions - see "Known Issues" below.)

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
