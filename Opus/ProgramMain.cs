using Opus.IO;
using Opus.Solution;
using Opus.Solution.Solver;
using System;

[assembly: log4net.Config.XmlConfigurator(Watch = false)]

namespace Opus
{
    public static class ProgramMain
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ProgramMain));

        public static int Main(string[] args)
        {
            sm_log.Info("Starting up");

            if (args.Length < 2)
            {
                sm_log.Error("Usage: Opus.exe <puzzle file> <solution file>");
                return 1;
            }

            string puzzleFile = args[0];
            string solutionFile = args[1];

            try
            {
                sm_log.Info($"Loading puzzle file \"{puzzleFile}\"");
                var puzzle = PuzzleReader.ReadPuzzle(puzzleFile);

                sm_log.Info($"Puzzle name: {puzzle.Name}");

                    var solver = new PuzzleSolver(puzzle);
                    var solution = solver.Solve();

            /*    var glyph1 = new Glyph(null, new Vector2(1, 2), 0, GlyphType.Equilibrium);
                var arm1 = new Arm(null, new Vector2(3, 5), 2, MechanismType.Arm1);
                var objects = new GameObject[] { glyph1, arm1 };

                var program = new Program();
                program.GetArmInstructions(arm1).AddRange(new[] { Instruction.Grab, Instruction.RotateClockwise, Instruction.Repeat });

                var solution = new PuzzleSolution(puzzle, objects, program);
                */
                sm_log.Info($"Writing solution to \"{solutionFile}\"");
                SolutionWriter.WriteSolution(solution, solutionFile);
            }
            catch (ParseException e)
            {
                sm_log.Error("Error loading puzzle file", e);
                return 1;
            }
            catch (SolverException e)
            {
                sm_log.Error("Unable to solve this puzzle", e);
                return 1;
            }
            catch (Exception e)
            {
                sm_log.Error("Internal error", e);
                return 1;
            }

            return 0;
        }
    }
}
