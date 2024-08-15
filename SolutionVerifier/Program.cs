using System;
using System.Collections.Generic;
using System.IO;

namespace SolutionVerifier
{
    internal class Program
    {
        private class Solution
        {
            public string PuzzleFile;
            public string SolutionFile;
        }

        static int Main(string[] args)
        {
            List<Solution> solutions;
            try
            {
                solutions = ParseArguments(args);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error parsing command line args: {e.Message}");
                ShowUsage();
                return 1;
            }

            try
            {
                VerifySolutions(solutions);
            }
            catch (Exception e)
            {
                Console.WriteLine("Internal error: " + e.ToString());
                return 1;
            }

            return 0;
        }

        private static List<Solution> ParseArguments(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("Wrong number of arguments");
            }

            var solutions = new List<Solution>();

            if (args[0] == "--batch")
            {
                string inputFile = args[1];
                using (var reader = new StreamReader(inputFile))
                {
                    string line;
                    int lineNumber = 1;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0)
                        {
                            continue;
                        }

                        var parts = line.Split(';');
                        if (parts.Length != 2)
                        {
                            throw new Exception($"Error reading line {lineNumber} from {inputFile}: line is not of the form <puzzle file>;<solution file>.");
                        }

                        solutions.Add(new Solution { PuzzleFile = parts[0], SolutionFile = parts[1] });
                        lineNumber++;
                    }
                }
            }
            else
            {
                solutions.Add(new Solution { PuzzleFile = args[0], SolutionFile = args[1] });
            }

            return solutions;
        }

        private static void VerifySolutions(IEnumerable<Solution> solutions)
        {
            foreach (var solution in solutions)
            {
                try
                {
                    using var verifier = new Verifier(solution.PuzzleFile, solution.SolutionFile);
                    var metrics = verifier.CalculateMetrics();
                    Console.WriteLine($"SOLUTION: {solution.SolutionFile}");
                    Console.WriteLine($"SUCCESS: {metrics.Cost}/{metrics.Cycles}/{metrics.Area}/{metrics.Instructions}");
                }
                catch (VerifierException e)
                {
                    Console.WriteLine($"SOLUTION: {solution.SolutionFile}");
                    Console.WriteLine($"ERROR: {e.Message}");
                }
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Verifies a solution to a puzzle.");
            Console.WriteLine("Usage: SolutionVerifier.exe <puzzle file> <solution file>");
            Console.WriteLine("or     SolutionVerifier.exe --batch <input file>");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("    --batch <input file>     Verifies multiple solutions. In this case, <input file> should be a file containing");
            Console.WriteLine("                             lines of the form: <puzzle file>;<solution file>");
        }
    }
}
