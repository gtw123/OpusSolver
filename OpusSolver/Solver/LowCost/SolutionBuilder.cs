using OpusSolver.Solver.LowCost.Input;
using OpusSolver.Solver.LowCost.Output;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    public class SolutionBuilder : ISolutionBuilder
    {
        private readonly Puzzle m_puzzle;
        private readonly Recipe m_recipe;
        private readonly ProgramWriter m_writer;

        private MoleculeDisassemblerFactory m_disassemblerFactory;
        private MoleculeAssemblerFactory m_assemblerFactory;

        private ArmArea m_armArea;
        private List<LowCostAtomGenerator> m_atomGenerators = [];

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
                m_puzzle.Reagents.ToDictionary(p => p.ID, p => m_disassemblerFactory.GetReagentElementInfo(p)),
                m_puzzle.Products.ToDictionary(p => p.ID, p => m_assemblerFactory.GetProductElementInfo(p)),
                useSharedElementBuffer: true,
                usePendingElementsInOrder: false);
        }

        public void CreateAtomGenerators(ElementPipeline pipeline)
        {
            m_armArea = new ArmArea(null, m_writer);

            var baseTransform = new Transform2D();
            var offsetTransform = new Transform2D(new Vector2(m_armArea.ArmLength, 0), HexRotation.R0);

            // Process the generators in reverse so that we start with the output generator
            var elementGenerators = pipeline.ElementGenerators;
            foreach (var elementGenerator in Enumerable.Reverse(elementGenerators))
            {
                var atomGenerator = CreateAtomGenerator(elementGenerator, baseTransform.Apply(offsetTransform));
                m_atomGenerators.Add(atomGenerator);

                if (!atomGenerator.IsEmpty)
                {
                    var positionOffset = (atomGenerator.RequiredWidth - 1) * new Vector2(1, -1).RotateBy(baseTransform.Rotation);
                    atomGenerator.Transform.Position += positionOffset;
                    baseTransform.Position += positionOffset;
                    baseTransform.Rotation = baseTransform.Rotation.Rotate60Clockwise();
                }
            }

            var requiredAccessPoints = Enumerable.Reverse(m_atomGenerators).SelectMany(g => g.RequiredAccessPoints.Select(p => g.GetWorldTransform().Apply(p)));
            m_armArea.CreateComponents(requiredAccessPoints);
        }

        private LowCostAtomGenerator CreateAtomGenerator(ElementGenerator elementGenerator, Transform2D transform)
        {
            LowCostAtomGenerator atomGenerator = elementGenerator switch
            {
                ElementGenerators.InputGenerator inputGenerator => CreateInputArea(inputGenerator),
                ElementGenerators.OutputGenerator => new OutputArea(m_writer, m_armArea, m_assemblerFactory),
                ElementGenerators.SingleStackElementBuffer elementBuffer => CreateAtomBuffer(elementBuffer.GetBufferInfo()),
                ElementGenerators.MetalProjectorGenerator => new MetalProjector(m_writer, m_armArea),
                ElementGenerators.MetalPurifierGenerator metalPurifier => new MetalPurifier(m_writer, m_armArea, metalPurifier.Sequences),
                ElementGenerators.MorsVitaeGenerator => new MorsVitaeGenerator(m_writer, m_armArea),
                ElementGenerators.QuintessenceDisperserGenerator => new QuintessenceDisperser(m_writer, m_armArea),
                ElementGenerators.QuintessenceGenerator => new QuintessenceGenerator(m_writer, m_armArea),
                ElementGenerators.SaltGenerator saltGenerator => saltGenerator.RequiresCardinalPassThrough ? new SaltGenerator(m_writer, m_armArea) : new SaltGeneratorNoCardinalPassThrough(m_writer, m_armArea),
                ElementGenerators.VanBerloGenerator => new VanBerloGenerator(m_writer, m_armArea),
                _ => throw new ArgumentException($"Unknown element generator type {elementGenerator.GetType()}")
            };

            elementGenerator.AtomGenerator = atomGenerator;
            atomGenerator.Parent = m_armArea;
            atomGenerator.Transform = transform;

            return atomGenerator;
        }

        private LowCostAtomGenerator CreateInputArea(ElementGenerators.InputGenerator generator)
        {
            var reagents = generator.Inputs.Select(i => i.Molecule);
            if (reagents.All(r => r.Atoms.Count() == 1))
            {
                if (reagents.Count() <= SimpleInputArea.MaxReagents)
                {
                    return new SimpleInputArea(m_writer, m_armArea, reagents);
                }

                throw new UnsupportedException($"LowCost solver can't currently handle more than {SimpleInputArea.MaxReagents} monoatomic reagents (requested {reagents.Count()}).");
            }
            else if (reagents.All(r => r.Atoms.Count() <= 2))
            {
                if (reagents.Count() <= DiatomicInputArea.MaxReagents)
                {
                    return new DiatomicInputArea(m_writer, m_armArea, reagents);
                }

                throw new UnsupportedException($"LowCost solver can't currently handle more than {DiatomicInputArea.MaxReagents} diatomic reagents (requested {reagents.Count()}: {string.Join(", ", reagents.Select(r => r.Atoms.Count()))}).");
            }
            else if (reagents.All(r => r.Height == 1))
            {
                if (reagents.Count() <= LinearInputArea.MaxReagents)
                {
                    return new LinearInputArea(m_writer, m_armArea, reagents);
                }

                throw new UnsupportedException($"LowCost solver can't currently handle more than {LinearInputArea.MaxReagents} linear reagents (requested {reagents.Count()}: {string.Join(", ", reagents.Select(r => r.Atoms.Count()))}).");
            }

            throw new UnsupportedException("LowCost solver can't currently handle non-linear reagents with more than two atoms.");
        }

        private LowCostAtomGenerator CreateAtomBuffer(ElementGenerators.SingleStackElementBuffer.BufferInfo bufferInfo)
        {
            if (bufferInfo.Elements.Count == 0)
            {
                return new DummyAtomGenerator(m_writer, m_armArea);
            }

            if (bufferInfo.Elements.All(e => e.IsStored) && m_puzzle.AllowedGlyphs.Contains(GlyphType.Disposal))
            {
                return new WasteDisposer(m_writer, m_armArea);
            }
            else
            {
                return new AtomBuffer(m_writer, m_armArea, bufferInfo);
            }
        }

        public IEnumerable<GameObject> GetAllObjects()
        {
            return m_armArea.GetAllObjects();
        }
    }
}