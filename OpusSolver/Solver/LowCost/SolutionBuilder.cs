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
            var currentRotation = HexRotation.R0;
            var outputGenerator = elementGenerators.OfType<ElementGenerators.OutputGenerator>().Single();
            CreateAtomGenerator(outputGenerator, currentRotation);
            currentRotation = currentRotation.Rotate60Clockwise();

            var vanBerlo = elementGenerators.OfType<ElementGenerators.VanBerloGenerator>().SingleOrDefault();
            if (vanBerlo != null)
            {
                CreateAtomGenerator(vanBerlo, currentRotation);
                currentRotation = currentRotation.Rotate60Clockwise();
            }

            var inputGenerator = elementGenerators.OfType<ElementGenerators.InputGenerator>().Single();
            CreateAtomGenerator(inputGenerator, currentRotation);
            currentRotation = currentRotation.Rotate60Clockwise();

            foreach (var elementBuffer in elementGenerators.OfType<ElementGenerators.ElementBuffer>())
            {
                CreateAtomGenerator(elementBuffer, HexRotation.R0);
            }
        }

        private AtomGenerator CreateAtomGenerator(ElementGenerator elementGenerator, HexRotation rotation)
        {
            var atomGenerator = elementGenerator switch
            {
                ElementGenerators.InputGenerator inputGenerator => CreateInputArea(inputGenerator),
                ElementGenerators.OutputGenerator => new OutputArea(m_writer, m_armArea, m_assemblerFactory),
                ElementGenerators.ElementBuffer elementBuffer => new AtomBuffer(m_writer, elementBuffer.StackInfos),
                ElementGenerators.MetalProjectorGenerator => throw new NotImplementedException("MetalProjector"),
                ElementGenerators.MetalPurifierGenerator metalPurifier => throw new NotImplementedException("MetalPurifier"),
                ElementGenerators.MorsVitaeGenerator => throw new NotImplementedException("MorsVitae"),
                ElementGenerators.QuintessenceDisperserGenerator => throw new NotImplementedException("QuintessenceDisperser"),
                ElementGenerators.QuintessenceGenerator => throw new NotImplementedException("QuintessenceGenerator"),
                ElementGenerators.SaltGenerator saltGenerator => throw new NotImplementedException("SaltGenerator"),
                ElementGenerators.VanBerloGenerator => new VanBerloGenerator(m_writer, m_armArea),
                _ => throw new ArgumentException($"Unknown element generator type {elementGenerator.GetType()}")
            };

            elementGenerator.AtomGenerator = atomGenerator;
            atomGenerator.Parent = m_armArea;
            atomGenerator.Transform.Position = new Vector2(m_armArea.ArmLength, 0).RotateBy(rotation);
            atomGenerator.Transform.Rotation = rotation;

            return atomGenerator;
        }

        private AtomGenerator CreateInputArea(ElementGenerators.InputGenerator generator)
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