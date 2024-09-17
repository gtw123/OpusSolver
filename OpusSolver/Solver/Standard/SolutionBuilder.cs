﻿using OpusSolver.Solver.Standard.Input;
using OpusSolver.Solver.Standard.Output;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard
{
    public class SolutionBuilder : ISolutionBuilder
    {
        private readonly Puzzle m_puzzle;
        private readonly Recipe m_recipe;
        private readonly ProgramWriter m_writer;

        private MoleculeDisassemblerFactory m_disassemblerFactory;
        private MoleculeAssemblerFactory m_assemblerFactory;

        private AtomGenerator m_rootAtomGenerator;

        public SolutionBuilder(Puzzle puzzle, Recipe recipe, ProgramWriter writer)
        {
            m_puzzle = puzzle;
            m_recipe = recipe;
            m_writer = writer;

            m_disassemblerFactory = new MoleculeDisassemblerFactory(puzzle.Reagents);
            m_assemblerFactory = new MoleculeAssemblerFactory(puzzle.Products);
        }

        public SolutionPlan CreatePlan()
        {
            return new SolutionPlan(m_puzzle, m_recipe,
                m_puzzle.Reagents.ToDictionary(p => p.ID, p => m_disassemblerFactory.GetReagentElementOrder(p)),
                m_puzzle.Products.ToDictionary(p => p.ID, p => m_assemblerFactory.GetProductElementOrder(p)),
                allowPassthroughWithPendingElements: false);
        }

        public void CreateAtomGenerators(ElementPipeline pipeline)
        {
            foreach (var elementGenerator in pipeline.ElementGenerators)
            {
                var atomGenerator = CreateAtomGenerator(elementGenerator);
                elementGenerator.AtomGenerator = atomGenerator;

                var parentGenerator = elementGenerator.Parent;
                if (parentGenerator != null)
                {
                    atomGenerator.Parent = parentGenerator.AtomGenerator;
                    atomGenerator.Transform.Position = parentGenerator.AtomGenerator.OutputPosition;
                }
                else
                {
                    m_rootAtomGenerator = atomGenerator;
                }
            }
        }

        private AtomGenerator CreateAtomGenerator(ElementGenerator elementGenerator)
        {
            return elementGenerator switch
            {
                ElementGenerators.InputGenerator inputGenerator => CreateInputArea(inputGenerator),
                ElementGenerators.OutputGenerator => new SimpleOutputArea(m_writer, m_assemblerFactory),
                ElementGenerators.ElementBuffer elementBuffer => new AtomBuffer(m_writer, elementBuffer.StackInfos),
                ElementGenerators.MetalProjectorGenerator => new MetalProjector(m_writer),
                ElementGenerators.MetalPurifierGenerator metalPurifier => new MetalPurifier(m_writer, metalPurifier.Sequences),
                ElementGenerators.MorsVitaeGenerator => new MorsVitaeGenerator(m_writer),
                ElementGenerators.QuintessenceDisperserGenerator => new QuintessenceDisperser(m_writer),
                ElementGenerators.QuintessenceGenerator => new QuintessenceGenerator(m_writer),
                ElementGenerators.SaltGenerator saltGenerator => saltGenerator.RequiresCardinalPassThrough ? new SaltGenerator(m_writer) : new SaltGeneratorNoCardinalPassThrough(m_writer),
                ElementGenerators.VanBerloGenerator => new VanBerloGenerator(m_writer),
                _ => throw new ArgumentException($"Unknown element generator type {elementGenerator.GetType()}")
            };
        }

        private AtomGenerator CreateInputArea(ElementGenerators.InputGenerator generator)
        {
            var reagents = generator.Inputs.Select(i => i.Molecule);
            if (reagents.All(r => r.Atoms.Count() == 1))
            {
                if (reagents.Count() <= SimpleInputArea.MaxReagents)
                {
                    return new SimpleInputArea(m_writer, reagents);
                }
            }

            return new ComplexInputArea(m_writer, reagents, m_disassemblerFactory);
        }

        public IEnumerable<GameObject> GetAllObjects()
        {
            return m_rootAtomGenerator.GetAllObjects();
        }
    }
}
