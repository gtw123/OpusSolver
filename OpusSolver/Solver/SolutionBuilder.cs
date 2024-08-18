using OpusSolver.Solver.AtomGenerators;
using OpusSolver.Solver.AtomGenerators.Input;
using OpusSolver.Solver.AtomGenerators.Output;
using System;
using System.Linq;

namespace OpusSolver.Solver
{
    public class SolutionBuilder
    {
        private ProgramWriter m_writer;

        public SolutionBuilder(ProgramWriter writer)
        {
            m_writer = writer;
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
            }
        }

        private AtomGenerator CreateAtomGenerator(ElementGenerator elementGenerator)
        {
            return elementGenerator switch
            {
                ElementGenerators.InputGenerator inputGenerator => CreateInputGenerator(inputGenerator),
                ElementGenerators.OutputGenerator outputGenerator => new SimpleOutputArea(m_writer, outputGenerator.AssemblyStrategy),
                ElementGenerators.ElementBuffer elementBuffer => new AtomBuffer(m_writer, elementBuffer.StackInfos),
                ElementGenerators.MetalProjectorGenerator => new MetalProjector(m_writer),
                ElementGenerators.MetalPurifierGenerator metalPurifier => new MetalPurifier(m_writer, metalPurifier.Sequences),
                ElementGenerators.MorsVitaeGenerator => new MorsVitaeGenerator(m_writer),
                ElementGenerators.QuintessenceDisperserGenerator => new QuintessenceDisperser(m_writer),
                ElementGenerators.QuintessenceGenerator => new QuintessenceGenerator(m_writer),
                ElementGenerators.SaltGenerator saltGenerator => saltGenerator.RequiresPassThrough ? new SaltGenerator(m_writer) : new SaltGeneratorNoPassThrough(m_writer),
                ElementGenerators.VanBerloGenerator => new VanBerloGenerator(m_writer),
                _ => throw new ArgumentException($"Unknown element generator type {elementGenerator.GetType()}")
            };
        }

        private AtomGenerator CreateInputGenerator(ElementGenerators.InputGenerator generator)
        {
            var strategies = generator.DisassemblyStrategies;
            if (strategies.All(s => s.Molecule.Atoms.Count() == 1))
            {
                if (strategies.Count() <= SimpleInputArea.MaxReagents)
                {
                    return new SimpleInputArea(m_writer, strategies.Select(s => s.Molecule));
                }
            }

            return new ComplexInputArea(m_writer, strategies);
        }
    }
}
