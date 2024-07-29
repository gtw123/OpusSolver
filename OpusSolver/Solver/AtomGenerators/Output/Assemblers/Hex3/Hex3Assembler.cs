﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers.Hex3
{
    /// <summary>
    /// Assembles molecules that fit within a hexagon of diagonal length 3, e.g.
    ///   O - O
    ///  / \ / \
    /// O - O - O
    ///  \ / \ /
    ///   O - O
    /// </summary>
    public class Hex3Assembler : MoleculeAssembler
    {
        public override Vector2 OutputPosition => new Vector2();

        private AssemblyArea m_assemblyArea;

        private readonly Dictionary<int, MoleculeBuilder> m_moleculeBuilders = new();
        private readonly Dictionary<int, LoopingCoroutine<object>> m_assembleCoroutines = new();

        private class ProductOutput
        {
            public int OutputIndex;
            public bool UseSimpleOutput;
        }

        private readonly Dictionary<int, ProductOutput> m_outputs = new();

        private readonly List<Arm> m_rightOutputArms;
        private readonly List<Arm> m_leftOutputArms;

        public Hex3Assembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer)
        {
            m_assemblyArea = new AssemblyArea(this, writer);

            foreach (var product in products)
            {
                MoleculeBuilder builder;
                var centralAtoms = GetCentralAtoms(product);
                if (centralAtoms.Any())
                {
                    builder = new CenterAtomMoleculeBuilder(m_assemblyArea, product, centralAtoms);
                }
                else
                {
                    builder = new MissingCenterAtomMoleculeBuilder(m_assemblyArea, product);
                }

                m_moleculeBuilders[product.ID] = builder;
                m_assembleCoroutines[product.ID] = new LoopingCoroutine<object>(() => Assemble(builder));
            }

            var lefthandProducts = products.Where(p => m_moleculeBuilders[p.ID].OutputLocation == OutputLocation.Left);
            m_leftOutputArms = AddOutputs(lefthandProducts, new Vector2(-1, -3), new Vector2(0, -3), -HexRotation.R60);
            m_assemblyArea.IsLeftBonderUsed = lefthandProducts.Any();

            var righthandProducts = products.Except(lefthandProducts);
            m_rightOutputArms = AddOutputs(righthandProducts, new Vector2(3, -3), new Vector2(3, -3), HexRotation.R60);
        }

        private List<Arm> AddOutputs(IEnumerable<Molecule> products, Vector2 initialPosition, Vector2 offset, HexRotation rotationOffset)
        {
            var arms = new List<Arm>();
            var currentRotationOffset = HexRotation.R0;

            bool allSimpleOutputs = products.All(p => m_moleculeBuilders[p.ID].OutputLocation == OutputLocation.RightSimple);
			
            // Build the products in reverse order so that the final product is closer to the assembly area (saves a few cycles)
            int index = 0;
            foreach (var product in products.Reverse())
            {
                var builder = m_moleculeBuilders[product.ID];

                // Offset so the the center of the molecule is at (0, 0) (need to do this before rotating it)
                var transform = new Transform2D(-builder.CenterAtomPosition, HexRotation.R0);
                var rotation = builder.OutputRotation + (builder.RequiresRotationsBetweenOutputPositions ? currentRotationOffset : HexRotation.R0);

                var productCenter = initialPosition + index * offset;

                // We only allow simple outputs for the first product, or if all products are simple outputs (otherwise
                // we'd need to move all the output arms based on which product is being transported through them)
                bool useSimpleOutput = builder.OutputLocation == OutputLocation.RightSimple && (index == 0 || allSimpleOutputs);
                if (useSimpleOutput)
                {
                    productCenter += new Vector2(0, 1);
                }

                m_outputs[product.ID] = new ProductOutput { OutputIndex = index, UseSimpleOutput = useSimpleOutput };

                transform = new Transform2D(productCenter + builder.OutputPositionOffset, rotation).Apply(transform);
                new Product(this, transform.Position, transform.Rotation, product);

                if (index > 0)
                {
                    var armRotation = (rotationOffset == HexRotation.R60) ? HexRotation.R180 : HexRotation.R0;
                    arms.Add(new Arm(this, productCenter - offset - new Vector2(3, 0).RotateBy(armRotation), armRotation, ArmType.Arm1, 3));
                }

                currentRotationOffset += rotationOffset;
                index++;
            }

            return arms;
        }

        public override IEnumerable<Element> GetProductElementOrder(Molecule product)
        {
            return m_moleculeBuilders[product.ID].GetElementsInBuildOrder();
        }

        public override void AddAtom(Element element, int productID)
        {
            m_assembleCoroutines[productID].Next();
        }

        private IEnumerable<object> Assemble(MoleculeBuilder builder)
        {
            Writer.AppendFragment(builder.Fragments.First());

            foreach (var fragment in builder.Fragments.Skip(1))
            {
                yield return null;
                Writer.AppendFragment(fragment);
            }

            switch (builder.OutputLocation)
            {
                case OutputLocation.Left:
                    MoveProductToLefthandOutput(builder.Product);
                    break;
                case OutputLocation.Right:
                    MoveProductToRighthandOutput(builder.Product);
                    break;
                case OutputLocation.RightSimple:
                    bool useSimpleOutput = m_outputs[builder.Product.ID].UseSimpleOutput;
                    MoveProductToRighthandOutput(builder.Product, moveAfterRotate: !useSimpleOutput);
                    break;
                case OutputLocation.RightNoCenter:
                    MoveProductToOutput(builder.Product, m_rightOutputArms, [Instruction.RotateCounterclockwise, Instruction.PivotClockwise]);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown output location {builder.OutputLocation}");
            }

            yield return null;
        }

        private void MoveProductToRighthandOutput(Molecule product, bool moveAfterRotate = false)
        {
            Instruction[] instructions = moveAfterRotate ? [Instruction.RotateClockwise, Instruction.MovePositive, Instruction.Reset]
                : [Instruction.RotateClockwise, Instruction.Reset];

            Writer.Write(m_assemblyArea.AssemblyArm, instructions);
            Writer.AdjustTime(-1);
            MoveProductToOutput(product, m_rightOutputArms, [Instruction.RotateCounterclockwise]);
        }

        private void MoveProductToLefthandOutput(Molecule product)
        {
            Writer.Write(m_assemblyArea.AssemblyArm, [Instruction.RotateCounterclockwise, Instruction.Reset]);
            Writer.AdjustTime(-1);
            MoveProductToOutput(product, m_leftOutputArms, [Instruction.RotateClockwise]);
        }

        private void MoveProductToOutput(Molecule product, List<Arm> outputArms, IEnumerable<Instruction> armInstructions)
        {
            int outputIndex = m_outputs[product.ID].OutputIndex;
            if (outputIndex > 0)
            {
                for (int i = 0; i < outputIndex; i++)
                {
                    Writer.WriteGrabResetAction(outputArms[i], armInstructions);
                }
            }
        }

        public override void OptimizeParts()
        {
            m_assemblyArea.OptimizeParts();
        }

        public static bool IsProductCompatible(Molecule product)
        {
            if (product.Size > 3)
            {
                return false;
            }

            if (product.Width == 3 && product.Height == 3 && product.DiagonalLength == 3 && product.GetAtom(new Vector2(0, 0)) != null)
            {
                // This is a 3x3 triangle which is bigger than a 3x3 hex
                return false;
            }

            return true;
        }

        private static IEnumerable<Atom> GetCentralAtoms(Molecule product)
        {
            bool IsCentralAtom(Atom atom) => product.Atoms.All(atom2 => atom == atom2 || product.AreAtomsAdjacent(atom, atom2));
            return product.Atoms.Where(atom => IsCentralAtom(atom));
        }
    }
}