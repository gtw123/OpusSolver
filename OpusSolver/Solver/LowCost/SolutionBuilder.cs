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
        private SolutionParameterSet m_paramSet;
        private readonly ProgramWriter m_writer;

        private readonly List<Molecule> m_requiredReagents;
        private MoleculeDisassemblerFactory m_disassemblerFactory;
        private MoleculeAssemblerFactory m_assemblerFactory;

        private ArmArea m_armArea;
        private List<LowCostAtomGenerator> m_atomGenerators = [];

        public SolutionBuilder(Puzzle puzzle, Recipe recipe, SolutionParameterSet paramSet, ProgramWriter writer)
        {
            m_puzzle = puzzle;
            m_recipe = recipe;
            m_paramSet = paramSet;
            m_writer = writer;

            m_requiredReagents = puzzle.Reagents.Where(r => recipe.HasAvailableReactions(ReactionType.Reagent, id: r.ID)).ToList();
            m_disassemblerFactory = new MoleculeDisassemblerFactory(m_requiredReagents, m_paramSet);
            m_assemblerFactory = new MoleculeAssemblerFactory(puzzle.Products, m_paramSet);
        }

        public SolutionPlan CreatePlan()
        {
            return new SolutionPlan(m_puzzle, m_recipe, m_paramSet,
                m_requiredReagents,
                m_requiredReagents.ToDictionary(p => p.ID, p => m_disassemblerFactory.GetReagentElementInfo(p)),
                m_puzzle.Products.ToDictionary(p => p.ID, p => m_assemblerFactory.GetProductElementInfo(p)),
                useSharedElementBuffer: true,
                usePendingElementsInOrder: false);
        }

        public void CreateAtomGenerators(ElementPipeline pipeline)
        {
            int armLength = m_paramSet.GetParameterValue(SolutionParameters.UseLength3Arm) ? 3 : 2;
            m_armArea = new ArmArea(null, m_writer, armLength);

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

            RegisterObjectsOnGrid();
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
            var usedReagents = generator.Inputs.Select(i => i.Molecule);
            return m_disassemblerFactory.CreateDisassembler(m_writer, m_armArea, usedReagents);
        }

        private LowCostAtomGenerator CreateAtomBuffer(ElementGenerators.SingleStackElementBuffer.BufferInfo bufferInfo)
        {
            if (bufferInfo.Elements.Count == 0)
            {
                return new DummyAtomGenerator(m_writer, m_armArea);
            }

            if (bufferInfo.Elements.All(e => e.IsWaste) && m_puzzle.AllowedGlyphs.Contains(GlyphType.Disposal))
            {
                return new WasteDisposer(m_writer, m_armArea);
            }
            else if (bufferInfo.Elements.All(e => !e.IsWaste))
            {
                return new AtomBufferNoWaste(m_writer, m_armArea, bufferInfo);
            }
            else
            {
                return new AtomBufferWithWaste(m_writer, m_armArea, bufferInfo);
            }
        }

        public IEnumerable<GameObject> GetAllObjects()
        {
            return m_armArea.GetAllObjects();
        }

        private void RegisterObjectsOnGrid()
        {
            foreach (var glyph in GetAllObjects().OfType<Glyph>())
            {
                m_armArea.GridState.RegisterGlyph(glyph);
            }

            foreach (var reagent in GetAllObjects().OfType<Reagent>())
            {
                m_armArea.GridState.RegisterReagent(reagent);
            }

            var trackCells = new HashSet<Vector2>();
            foreach (var track in GetAllObjects().OfType<Track>())
            {
                m_armArea.GridState.RegisterTrack(track);
                trackCells.UnionWith(track.GetAllPathCells());
            }

            foreach (var arm in GetAllObjects().OfType<Arm>())
            {
                if (!trackCells.Contains(arm.GetWorldTransform().Position))
                {
                    m_armArea.GridState.RegisterStaticArm(arm);
                }
            }
        }

        public SolutionParameterRegistry GetAvailableParameters()
        {
            var registry = new SolutionParameterRegistry();

            registry.AddParameter(SolutionParameters.UseLength3Arm);

            if (m_puzzle.Products.Count > 1)
            {
                registry.AddParameter(SolutionParameterRegistry.Common.ReverseProductBuildOrder);
            }

            if (m_puzzle.Products.Any(p => p.Atoms.Count() > 1))
            {
                registry.AddParameter(SolutionParameterRegistry.Common.ReverseProductElementOrder);
            }

            if (m_puzzle.Reagents.Any(p => p.Atoms.Count() > 1))
            {
                registry.AddParameter(SolutionParameterRegistry.Common.ReverseReagentElementOrder);
            }

            bool IsSingleChain(Molecule molecule) => molecule.Atoms.All(a => a.BondCount <= 2) && molecule.Atoms.Count(a => a.BondCount == 1) == 2;
            if (m_puzzle.Reagents.Any(p => !IsSingleChain(p)))
            {
                registry.AddParameter(SolutionParameters.ReverseReagentBondTraversalDirection);
            }

            if (m_puzzle.Products.Any(p => !IsSingleChain(p)))
            {
                registry.AddParameter(SolutionParameters.UseBreadthFirstOrderForComplexProducts);
                registry.AddParameter(SolutionParameters.ReverseProductBondTraversalDirection);
            }

            return registry;
        }
    }
}