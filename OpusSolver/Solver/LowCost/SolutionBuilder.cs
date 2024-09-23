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
                m_puzzle.Reagents.ToDictionary(p => p.ID, p => m_disassemblerFactory.GetReagentElementOrder(p)),
                m_puzzle.Products.ToDictionary(p => p.ID, p => m_assemblerFactory.GetProductElementOrder(p)),
                usePendingElementsInOrder: false);
        }

        public void CreateAtomGenerators(ElementPipeline pipeline)
        {
            m_armArea = new ArmArea(null, m_writer);

            var baseTransform = new Transform2D();
            var offsetTransform = new Transform2D(new Vector2(m_armArea.ArmLength, 0), HexRotation.R0);

            // Process the generators in reverse so that we start with the output generator
            var elementGenerators = pipeline.ElementGenerators;
            foreach (var elementGenerator in Enumerable.Reverse(elementGenerators).Where(e => e is not ElementGenerators.ElementBuffer))
            {
                var atomGenerator = CreateAtomGenerator(elementGenerator, baseTransform.Apply(offsetTransform));
                m_atomGenerators.Add(atomGenerator);

                var positionOffset = (atomGenerator.RequiredWidth - 1) * new Vector2(1, -1).RotateBy(baseTransform.Rotation);
                atomGenerator.Transform.Position += positionOffset;
                baseTransform.Position += positionOffset;
                baseTransform.Rotation = baseTransform.Rotation.Rotate60Clockwise();
            }

            foreach (var elementBuffer in elementGenerators.OfType<ElementGenerators.ElementBuffer>())
            {
                CreateAtomGenerator(elementBuffer, baseTransform.Apply(offsetTransform));
            }

            var requiredAccessPoints = Enumerable.Reverse(m_atomGenerators).SelectMany(g => g.RequiredAccessPoints.Select(p => g.GetWorldTransform().Apply(p)));
            var additionalAccessPoints = m_atomGenerators.SelectMany(g => g.AdditionalAccessPoints.Select(p => g.GetWorldTransform().Apply(p)));
            m_armArea.CreateComponents(additionalAccessPoints.Concat(requiredAccessPoints));
        }

        private LowCostAtomGenerator CreateAtomGenerator(ElementGenerator elementGenerator, Transform2D transform)
        {
            LowCostAtomGenerator atomGenerator = elementGenerator switch
            {
                ElementGenerators.InputGenerator inputGenerator => CreateInputArea(inputGenerator),
                ElementGenerators.OutputGenerator => new OutputArea(m_writer, m_armArea, m_assemblerFactory),
                ElementGenerators.ElementBuffer elementBuffer => new AtomBuffer(m_writer, m_armArea, elementBuffer.GetBufferInfo(), m_atomGenerators.OfType<IWasteDisposer>().SingleOrDefault()),
                ElementGenerators.MetalProjectorGenerator => new MetalProjector(m_writer, m_armArea),
                ElementGenerators.MetalPurifierGenerator metalPurifier => new MetalPurifier(m_writer, m_armArea, metalPurifier.Sequences),
                ElementGenerators.MorsVitaeGenerator => new MorsVitaeGenerator(m_writer, m_armArea),
                ElementGenerators.QuintessenceDisperserGenerator => new QuintessenceDisperser(m_writer, m_armArea),
                ElementGenerators.QuintessenceGenerator => new QuintessenceGenerator(m_writer, m_armArea),
                ElementGenerators.SaltGenerator saltGenerator => saltGenerator.RequiresCardinalPassThrough ? new SaltGenerator(m_writer, m_armArea) : new SaltGeneratorNoCardinalPassThrough(m_writer, m_armArea),
                ElementGenerators.VanBerloGenerator => new VanBerloGenerator(m_writer, m_armArea),
                ElementGenerators.WasteDisposer => CreateWasteDisposer(),
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

                throw new UnsupportedException($"LowCost solver can't currently handle more than {SimpleInputArea.MaxReagents} monoatomic reagents.");
            }

            throw new UnsupportedException("LowCost solver can't currently handle reagents with more than one atom.");
        }

        private LowCostAtomGenerator CreateWasteDisposer()
        {
            return m_puzzle.AllowedGlyphs.Contains(GlyphType.Disposal) ? new WasteDisposer(m_writer, m_armArea)
                : new WasteChainDisposer(m_writer, m_armArea);
        }

        public IEnumerable<GameObject> GetAllObjects()
        {
            return m_armArea.GetAllObjects();
        }
    }
}