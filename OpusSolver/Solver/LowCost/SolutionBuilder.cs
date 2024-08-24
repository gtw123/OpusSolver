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
                m_puzzle.Products.ToDictionary(p => p.ID, p => m_assemblerFactory.GetProductElementOrder(p)));
        }

        public void CreateAtomGenerators(ElementPipeline pipeline)
        {
            m_armArea = new ArmArea(null, m_writer);

            var elementGenerators = pipeline.ElementGenerators;

            // We always leave the output area unrotated because repeating molecules can't be rotated
            var baseTransform = new Transform2D();
            var offsetTransform = new Transform2D(new Vector2(m_armArea.ArmLength, 0), HexRotation.R0);

            var outputGenerator = elementGenerators.OfType<ElementGenerators.OutputGenerator>().Single();
            CreateAtomGenerator(outputGenerator, baseTransform.Apply(offsetTransform));
            baseTransform.Rotation = baseTransform.Rotation.Rotate60Clockwise();

            var metalProjector = elementGenerators.OfType<ElementGenerators.MetalProjectorGenerator>().SingleOrDefault();
            if (metalProjector != null)
            {
                baseTransform.Position += new Vector2(1, -1).RotateBy(baseTransform.Rotation);
                CreateAtomGenerator(metalProjector, baseTransform.Apply(offsetTransform));
                baseTransform.Rotation = baseTransform.Rotation.Rotate60Clockwise();
            }

            var vanBerlo = elementGenerators.OfType<ElementGenerators.VanBerloGenerator>().SingleOrDefault();
            if (vanBerlo != null)
            {
                CreateAtomGenerator(vanBerlo, baseTransform.Apply(offsetTransform));
                baseTransform.Rotation = baseTransform.Rotation.Rotate60Clockwise();
            }

            var saltGenerator = elementGenerators.OfType<ElementGenerators.SaltGenerator>().SingleOrDefault();
            if (saltGenerator != null)
            {
                baseTransform.Position += new Vector2(1, -1).RotateBy(baseTransform.Rotation);
                CreateAtomGenerator(saltGenerator, baseTransform.Apply(offsetTransform));
                baseTransform.Rotation = baseTransform.Rotation.Rotate60Clockwise();
            }

            var inputGenerator = elementGenerators.OfType<ElementGenerators.InputGenerator>().Single();
            CreateAtomGenerator(inputGenerator, baseTransform.Apply(offsetTransform));
            baseTransform.Rotation = baseTransform.Rotation.Rotate60Clockwise();

            foreach (var elementBuffer in elementGenerators.OfType<ElementGenerators.ElementBuffer>())
            {
                CreateAtomGenerator(elementBuffer, baseTransform.Apply(offsetTransform));
            }

            var requiredAccessPoints = Enumerable.Reverse(m_atomGenerators).SelectMany(g => g.RequiredAccessPoints.Select(p => g.GetWorldTransform().Apply(p)));
            m_armArea.CreateComponents(requiredAccessPoints);
        }

        private AtomGenerator CreateAtomGenerator(ElementGenerator elementGenerator, Transform2D transform)
        {
            LowCostAtomGenerator atomGenerator = elementGenerator switch
            {
                ElementGenerators.InputGenerator inputGenerator => CreateInputArea(inputGenerator),
                ElementGenerators.OutputGenerator => new OutputArea(m_writer, m_armArea, m_assemblerFactory),
                ElementGenerators.ElementBuffer elementBuffer => new AtomBuffer(m_writer, m_armArea, elementBuffer.StackInfos),
                ElementGenerators.MetalProjectorGenerator => new MetalProjector(m_writer, m_armArea),
                ElementGenerators.MetalPurifierGenerator metalPurifier => throw new NotImplementedException("MetalPurifier"),
                ElementGenerators.MorsVitaeGenerator => throw new NotImplementedException("MorsVitae"),
                ElementGenerators.QuintessenceDisperserGenerator => throw new NotImplementedException("QuintessenceDisperser"),
                ElementGenerators.QuintessenceGenerator => throw new NotImplementedException("QuintessenceGenerator"),
                ElementGenerators.SaltGenerator saltGenerator => new SaltGenerator(m_writer, m_armArea), // TODO: Support non-passthrough version too?
                ElementGenerators.VanBerloGenerator => new VanBerloGenerator(m_writer, m_armArea),
                _ => throw new ArgumentException($"Unknown element generator type {elementGenerator.GetType()}")
            };

            elementGenerator.AtomGenerator = atomGenerator;
            atomGenerator.Parent = m_armArea;

            atomGenerator.Transform.Position = transform.Position;
            atomGenerator.Transform.Rotation = transform.Rotation;

            m_atomGenerators.Add(atomGenerator);

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
            }

            throw new SolverException("LowCost solver can't currently handle reagents with more than one atom.");
        }

        public IEnumerable<GameObject> GetAllObjects()
        {
            return m_armArea.GetAllObjects();
        }
    }
}