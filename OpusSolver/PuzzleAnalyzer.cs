﻿using OpusSolver.IO;
using OpusSolver.Solver;
using OpusSolver.Solver.Standard.Input;
using OpusSolver.Solver.Standard.Output.Hex3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpusSolver
{
    public class PuzzleAnalyzer : IDisposable
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(PuzzleAnalyzer));

        private CommandLineArguments m_args;
        private StreamWriter m_reportWriter;

        private class MoleculeListInfo
        {
            public int MaxAtomsPerMolecule;
            public List<Molecule> MoleculesByAtomCount;

            public MoleculeListInfo(IEnumerable<Molecule> molecules)
            {
                MaxAtomsPerMolecule = molecules.Max(m => m.Atoms.Count());
                MoleculesByAtomCount = molecules.OrderBy(m => m.Atoms.Count()).ToList();
            }
        }

        private class PuzzleInfo
        {
            public Puzzle Puzzle;
            public string File;
            public MoleculeListInfo Reagents;
            public MoleculeListInfo Products;
            public Recipe Recipe;
        }

        private class MoleculeListComparer : IComparer<MoleculeListInfo>
        {
            public int Compare(MoleculeListInfo list1, MoleculeListInfo list2)
            {
                int diff = list1.MaxAtomsPerMolecule - list2.MaxAtomsPerMolecule;
                if (diff != 0)
                {
                    return diff;
                }

                diff = list1.MoleculesByAtomCount.Count - list2.MoleculesByAtomCount.Count;
                if (diff != 0)
                {
                    return diff;
                }

                for (int i = 0; i < list1.MoleculesByAtomCount.Count; i++)
                {
                    diff = list1.MoleculesByAtomCount[i].Atoms.Count() - list2.MoleculesByAtomCount[i].Atoms.Count();
                    if (diff != 0)
                    {
                        return diff;
                    }
                }

                return 0;
            }
        }

        public PuzzleAnalyzer(CommandLineArguments args)
        {
            m_args = args;
            m_reportWriter = new StreamWriter(m_args.ReportFile);
            m_reportWriter.WriteLine("Name,ReagentCount,Reagent1,Reagent2,Reagent3,Reagent4,ProductCount,Product1,Product2,Product3,Product4,");
        }

        public void Dispose()
        {
            m_reportWriter?.Dispose();
            m_reportWriter = null;
        }

        public void Analyze()
        {
            var puzzles = new List<PuzzleInfo>();
            foreach (var puzzleFile in m_args.PuzzleFiles)
            {
                puzzles.Add(LoadPuzzle(puzzleFile));
            }

            var orderedPuzzles = puzzles.OrderBy(p => p.Reagents, new MoleculeListComparer())
                .ThenBy(p => p.Recipe.GetAvailableReactionTypes().Count())
                .ThenBy(p => p.Products, new MoleculeListComparer());
            //var orderedPuzzles = puzzles.OrderBy(p => p.Products, new MoleculeListComparer()).ThenBy(p => p.Reagents, new MoleculeListComparer());

            foreach (var puzzleInfo in orderedPuzzles)
            {
                var puzzle = puzzleInfo.Puzzle;
                m_reportWriter.Write($"{puzzle.Name},");

                m_reportWriter.Write($"{puzzleInfo.Reagents.MoleculesByAtomCount.Count},");
                for (int i = 0; i < 4; i++)
                {
                    if (i < puzzleInfo.Reagents.MoleculesByAtomCount.Count)
                    {
                        m_reportWriter.Write(puzzleInfo.Reagents.MoleculesByAtomCount[i].Atoms.Count());
                    }
                    m_reportWriter.Write(",");
                }

                m_reportWriter.Write(",");

                m_reportWriter.Write($"{puzzleInfo.Products.MoleculesByAtomCount.Count},");
                for (int i = 0; i < 4; i++)
                {
                    if (i < puzzleInfo.Products.MoleculesByAtomCount.Count)
                    {
                        m_reportWriter.Write(puzzleInfo.Products.MoleculesByAtomCount[i].Atoms.Count());
                    }
                    m_reportWriter.Write(",");
                }

                m_reportWriter.Write(",");
                m_reportWriter.WriteLine();
                m_reportWriter.WriteLine();

                void WriteMolecules(IEnumerable<Molecule> molecules)
                {
                    var moleculeLines = new List<string>();
                    foreach (var reagent in molecules)
                    {
                        var lines = reagent.ToString().Split([Environment.NewLine], StringSplitOptions.None).ToList();
                        int padWidth = lines.Max(line => line.Length) + 3;

                        for (int i = 0; i < lines.Count; i++)
                        {
                            if (lines[i].Length < padWidth)
                            {
                                lines[i] += new string(' ', padWidth - lines[i].Length);
                            }

                            if (i >= moleculeLines.Count)
                            {
                                moleculeLines.Add("");
                            }

                            moleculeLines[i] += lines[i];
                        }
                    }

                    foreach (string line in moleculeLines)
                    {
                        m_reportWriter.WriteLine(line);
                    }
                }

                m_reportWriter.WriteLine("Reagents:");
                WriteMolecules(puzzleInfo.Reagents.MoleculesByAtomCount);
                m_reportWriter.WriteLine("Products:");
                WriteMolecules(puzzleInfo.Products.MoleculesByAtomCount);
                m_reportWriter.WriteLine(puzzleInfo.Recipe);
                m_reportWriter.WriteLine();
            }

            sm_log.Info($"Report saved to \"{m_args.ReportFile}\"");
        }

        private bool IsHex3Plus1(Molecule molecule)
        {
            for (int i = 0; i < molecule.Atoms.Count(); i++)
            {
                var atoms = molecule.Atoms.Select(a => a.Copy()).ToList();
                atoms.RemoveAt(i);

                var newMolecule = new Molecule(molecule.Type, atoms, molecule.ID);
                if (Hex3Assembler.IsProductCompatible(newMolecule))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsHex3Plus2(Molecule molecule)
        {
            for (int i = 0; i < molecule.Atoms.Count(); i++)
            {
                for (int j = 0; j < i; j++)
                {
                    var atoms = molecule.Atoms.Select(a => a.Copy()).ToList();
                    atoms.RemoveAt(i);
                    atoms.RemoveAt(j);

                    var newMolecule = new Molecule(molecule.Type, atoms, molecule.ID);
                    if (Hex3Assembler.IsProductCompatible(newMolecule))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private PuzzleInfo LoadPuzzle(string puzzleFile)
        {
            var puzzle = PuzzleReader.ReadPuzzle(puzzleFile);
            var generator = new RecipeGenerator(puzzle, new RecipeOptions());

            return new PuzzleInfo
            {
                Puzzle = puzzle,
                File = puzzleFile,
                Reagents = new MoleculeListInfo(puzzle.Reagents),
                Products = new MoleculeListInfo(puzzle.Products),
                Recipe = generator.GenerateRecipes(generateMultiple: false).First()
            };
        }
    }
}
