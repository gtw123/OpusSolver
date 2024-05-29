using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers
{
    /// <summary>
    /// Assembles molecules that have size of 3 or less. i.e. Those that fit within this hex:
    ///   O - O
    ///  / \ / \
    /// O - O - O
    ///  \ / \ /
    ///   O - O
    /// </summary>
    public class Hex3Assembler : MoleculeAssembler
    {
        private class ProductAssemblyInfo
        {
            public Molecule Product { get; private set; }

            public Atom CenterAtom => Product.GetAtom(new Vector2(1, 1));

            public enum OperationType
            {
                RotateClockwise,
                RotateCounterclockwise,
                GrabAtom,
                Bond,
                MoveAwayFromBonder,
                MoveTowardBonder,
            }
            public class Operation
            {
                public OperationType Type;
                public HexRotation FinalRotation;
                public Atom Atom;
            }

            public IEnumerable<Operation> CenterOperations { get; private set; }
            public IEnumerable<Operation> ClockwiseOperations { get; private set; }
            public IEnumerable<Operation> CounterclockwiseOperations { get; private set; }

            private bool m_needAvoidBondBeforeCounterclockwiseOperations = false;

            private readonly List<Operation> m_allOperations = new();

            private readonly HashSet<HexRotation> m_grabbedAtoms = new();
            private readonly HashSet<HexRotation> m_constructedClockwiseBonds = new(); // Whether a bond has been constructed in a clockwise direction from the atom in the specified radial direction

            public ProductAssemblyInfo(Molecule product)
            {
                Product = product;

                CenterOperations = GenerateCenterOperations().ToList();
                m_allOperations.AddRange(CenterOperations);

                ClockwiseOperations = GenerateClockwiseOperations().ToList();
                m_allOperations.AddRange(ClockwiseOperations);

                var counterclockwiseOperations = GenerateCounterclockwiseOperations().ToList();

                // TODO: Find a more elegant way to do this. Also, it might be possible to avoid it in some cases by pivoting before rotating to LH bonder.
                if (m_needAvoidBondBeforeCounterclockwiseOperations && counterclockwiseOperations.Any())
                {
                    var op = new Operation { Type = OperationType.MoveAwayFromBonder, FinalRotation = m_allOperations.Last().FinalRotation };
                    var temp = ClockwiseOperations.ToList();
                    temp.Add(op);
                    ClockwiseOperations = temp;
                    m_allOperations.Add(op);
                }

                CounterclockwiseOperations = counterclockwiseOperations;
                m_allOperations.AddRange(CounterclockwiseOperations);
            }

            public IEnumerable<Element> GetElementsInBuildOrder()
            {
                return new[] { CenterAtom.Element }.Concat(m_allOperations.Where(op => op.Type == OperationType.GrabAtom).Select(op => op.Atom.Element));
            }

            public HexRotation GetOutputRotation()
            {
                var rot = m_allOperations.Last().FinalRotation;
                if (CounterclockwiseOperations.Any())
                {
                    return HexRotation.R120 - rot;
                }
                else if (ClockwiseOperations.Any())
                {
                    return HexRotation.R120 - rot - HexRotation.R60;
                }
                else
                {
                    return HexRotation.R180 - rot - HexRotation.R60;
                }
            }

            private IEnumerable<Operation> GenerateCenterOperations()
            {
                // TODO: Handle molecules with no center atom
                // TODO: Handle molecules smaller than 3x3
                // Get the atoms that need to be bonded to the center atom
                var atoms = GetRadialAtomsWhere((dir, atom) => atom.Bonds[dir + HexRotation.R180] != BondType.None);

                // After we've bonded the radial atoms to the center atom, we'll move the molecule down one cell,
                // which means the top two atoms are on top of a bonder. This could create an unwanted bond,
                // so we try to choose the last radial atom so that we end up with at least one gap in the
                // top two cells.
                HexRotation lastAtomDir;
                if (atoms.Values.Count(a => a != null) != HexRotation.Count)
                {
                    lastAtomDir = HexRotation.All.FirstOrDefault(dir => atoms[dir] != null && atoms[dir.Rotate60Clockwise()] == null);
                }
                else
                {
                    // There are no empty gaps in the radial atoms, so instead try to make sure the top two
                    // atoms actually do need a bond, to avoid creating an unwanted bond. If there is no such
                    // atom then FirstOrDefault will return default(HexRotation) which is fine because in this
                    // case we won't even bother moving the molecule down one cell (it'll already be complete).
                    lastAtomDir = HexRotation.All.FirstOrDefault(dir => atoms[dir.Rotate60Clockwise()].Bonds[dir.Rotate180()] != BondType.None);
                }

                // Order the atoms so that lastAtomDir is last
                var currentDir = lastAtomDir.Rotate60Clockwise();
                var atomDir = currentDir;
                foreach (var rot in HexRotation.All)
                {
                    var atom = atoms[atomDir];
                    if (atom != null)
                    {
                        var (numRotations, rotationDir) = CalculateRotation(currentDir, atomDir);
                        for (int i = 0; i < numRotations; i++)
                        {
                            currentDir += rotationDir;
                            yield return new Operation
                            {
                                Type = (rotationDir == HexRotation.R60) ? OperationType.RotateClockwise : OperationType.RotateCounterclockwise,
                                FinalRotation = currentDir
                            };
                        }

                        m_grabbedAtoms.Add(atomDir);
                        yield return new Operation { Type = OperationType.GrabAtom, FinalRotation = currentDir, Atom = atom };
                    }

                    atomDir = atomDir.Rotate60Clockwise();
                }
            }

            /// <summary>
            /// Calculates the rotations required to get from currentDir to targetDir.
            /// </summary>
            private (int numRotations, HexRotation rotationDir) CalculateRotation(HexRotation currentDir, HexRotation targetDir)
            {
                var numRotations = (targetDir - currentDir).IntValue;
                var rotationDir = HexRotation.R60;
                if (numRotations >= 3)
                {
                    numRotations = HexRotation.Count - numRotations;
                    rotationDir = -HexRotation.R60;
                }

                return (numRotations, rotationDir);
            }


            private IEnumerable<Operation> GenerateClockwiseOperations()
            {
                // Get the atoms that need to be bonded to the next atom in a clockwise direction
                var atoms = GetRadialAtomsWhere((dir, atom) => atom.Bonds[dir - HexRotation.R120] != BondType.None);

                var currentDir = m_allOperations.Last().FinalRotation;
                currentDir = currentDir.Rotate60Clockwise();

                // Start with the atom next to the first one we've already grabbed
                var atomDir = HexRotation.All.FirstOrDefault(dir => m_grabbedAtoms.Contains(dir));
                atomDir = atomDir.Rotate60Counterclockwise();
                foreach (var rot in HexRotation.All)
                {
                    var atom = atoms[atomDir];
                    if (atom != null)
                    {
                        // We can only do anything if the next atom has already been grabbed
                        if (m_grabbedAtoms.Contains(atomDir.Rotate60Clockwise()))
                        {
                            var (numRotations, rotationDir) = CalculateRotation(currentDir, atomDir);

                            // Check if rotating without moving would create any unwanted bonds
                            var newDir = currentDir;
                            bool wouldCreateUnwantedBonds = false;
                            for (int i = 0; i < numRotations; i++)
                            {
                                newDir = newDir + rotationDir;
                                if (m_grabbedAtoms.Contains(newDir) && m_grabbedAtoms.Contains(newDir.Rotate60Clockwise()))
                                {
                                    if (atoms[newDir] == null)
                                    {
                                        wouldCreateUnwantedBonds = true;
                                        break;
                                    }
                                }
                            }

                            if (wouldCreateUnwantedBonds)
                            {
                                yield return new Operation { Type = OperationType.MoveAwayFromBonder, FinalRotation = currentDir }; 
                            }

                            for (int i = 0; i < numRotations; i++)
                            {
                                // make sure new dir + clockwise don't both have atoms and shouldn't have a bond
                                currentDir += rotationDir;
                                yield return new Operation
                                {
                                    Type = (rotationDir == HexRotation.R60) ? OperationType.RotateClockwise : OperationType.RotateCounterclockwise,
                                    FinalRotation = currentDir
                                };
                            }

                            if (wouldCreateUnwantedBonds)
                            {
                                yield return new Operation { Type = OperationType.MoveTowardBonder, FinalRotation = currentDir };
                            }

                            m_constructedClockwiseBonds.Add(atomDir);

                            if (!m_grabbedAtoms.Contains(atomDir))
                            {
                                m_grabbedAtoms.Add(atomDir);
                                yield return new Operation { Type = OperationType.GrabAtom, FinalRotation = currentDir, Atom = atom };
                            }
                            else
                            {
                                yield return new Operation { Type = OperationType.Bond, FinalRotation = currentDir, Atom = atom };
                            }
                        }
                    }

                    atomDir = atomDir.Rotate60Counterclockwise();
                }
            }

            private IEnumerable<Operation> GenerateCounterclockwiseOperations()
            {
                // Get the atoms that need to be bonded to the next atom in a counter-clockwise direction
                var atoms = GetRadialAtomsWhere((dir, atom) => atom.Bonds[dir + HexRotation.R120] != BondType.None);

                var currentDir = m_allOperations.Last().FinalRotation;
                if (!ClockwiseOperations.Any())
                {
                    // Account for the direction change that occurs when moving the molecule down one row at the start
                    // of clockwise operations. This is only necessary here when no clockwise operations are actually generated.
                    // TODO: Do this in a better way - keep track of current dir separately?
                    currentDir = currentDir.Rotate60Clockwise();
                }

                currentDir = currentDir - HexRotation.R120;

                if (m_grabbedAtoms.Contains(currentDir) && m_grabbedAtoms.Contains(currentDir.Rotate60Counterclockwise()))
                {
                    if (atoms[currentDir] == null)
                    {
                        m_needAvoidBondBeforeCounterclockwiseOperations = true;
                    }
                }

                // Start with the atom next to the first one we've already grabbed
                var atomDir = HexRotation.All.FirstOrDefault(dir => m_grabbedAtoms.Contains(dir));
                atomDir = atomDir.Rotate60Clockwise();
                bool isFirstOp = true;
                foreach (var rot in HexRotation.All)
                {
                    var atom = atoms[atomDir];
                    if (atom != null)
                    {
                        if (!m_constructedClockwiseBonds.Contains(atomDir.Rotate60Counterclockwise()))
                        {
                            var (numRotations, rotationDir) = CalculateRotation(currentDir, atomDir);

                            bool wouldCreateUnwantedBonds = false;
                            if (isFirstOp && m_needAvoidBondBeforeCounterclockwiseOperations)
                            {
                                // In this case we don't need to write the "move away" op, only the "move toward"
                                wouldCreateUnwantedBonds = true;
                            }
                            else
                            {
                                // Check if rotating without moving would create any unwanted bonds
                                var newDir = currentDir;
                                for (int i = 0; i < numRotations; i++)
                                {
                                    newDir = newDir + rotationDir;
                                    if (m_grabbedAtoms.Contains(newDir) && m_grabbedAtoms.Contains(newDir.Rotate60Counterclockwise()))
                                    {
                                        if (atoms[newDir] == null)
                                        {
                                            wouldCreateUnwantedBonds = true;
                                            break;
                                        }
                                    }
                                }

                                if (wouldCreateUnwantedBonds)
                                {
                                    yield return new Operation { Type = OperationType.MoveAwayFromBonder, FinalRotation = currentDir };
                                }
                            }

                            for (int i = 0; i < numRotations; i++)
                            {
                                currentDir += rotationDir;
                                yield return new Operation
                                {
                                    Type = (rotationDir == HexRotation.R60) ? OperationType.RotateClockwise : OperationType.RotateCounterclockwise,
                                    FinalRotation = currentDir
                                };
                            }

                            if (wouldCreateUnwantedBonds)
                            {
                                yield return new Operation { Type = OperationType.MoveTowardBonder, FinalRotation = currentDir };
                            }

                            m_constructedClockwiseBonds.Add(atomDir.Rotate60Counterclockwise());

                            if (!m_grabbedAtoms.Contains(atomDir))
                            {
                                m_grabbedAtoms.Add(atomDir);
                                yield return new Operation { Type = OperationType.GrabAtom, FinalRotation = currentDir, Atom = atom };
                            }
                            else
                            {
                                yield return new Operation { Type = OperationType.Bond, FinalRotation = currentDir, Atom = atom };
                            }

                            isFirstOp = false;
                        }
                    }

                    atomDir = atomDir.Rotate60Clockwise();
                }
            }

            private Dictionary<HexRotation, Atom> GetRadialAtomsWhere(Func<HexRotation, Atom, bool> includeAtom)
            {
                var result = new Dictionary<HexRotation, Atom>();
                foreach (var dir in HexRotation.All)
                {
                    var atom = Product.GetAdjacentAtom(CenterAtom.Position, dir);
                    result[dir] = atom != null && includeAtom(dir, atom) ? atom : null;
                }

                return result;
            }
        }

        public override Vector2 OutputPosition => new Vector2();

        private readonly Dictionary<int, ProductAssemblyInfo> m_assemblyInfo;
        private readonly LoopingCoroutine<object> m_assembleCoroutine;
        

        private readonly Arm m_horizontalArm;
        private readonly Arm m_assemblyArm;
        private readonly List<Arm> m_rightOutputArms = new();
        private readonly List<Arm> m_leftOutputArms = new();

        private ProductAssemblyInfo m_currentProductAssemblyInfo;

        private Dictionary<int, int> m_outputLocationsById = new();

        public Hex3Assembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer)
        {
            m_assemblyInfo = products.ToDictionary(p => p.ID, p => new ProductAssemblyInfo(p));

            m_assembleCoroutine = new LoopingCoroutine<object>(Assemble);

            // TODO: Include these only if needed
            new Glyph(this, new Vector2(0, 0), HexRotation.R0, GlyphType.Bonding);
            new Glyph(this, new Vector2(-2, 0), HexRotation.R0, GlyphType.Bonding);

            m_horizontalArm = new Arm(this, new Vector2(3, 0), HexRotation.R180, ArmType.Arm1, extension: 3);
            new Track(this, new Vector2(4, 0), HexRotation.R180, 2);

            m_assemblyArm = new Arm(this, new Vector2(1, -2), HexRotation.R60, ArmType.Arm1, extension: 2);
            new Track(this, new Vector2(1, -2), [
                new Track.Segment { Direction = HexRotation.R240, Length = 1 },
                new Track.Segment { Direction = HexRotation.R300, Length = 1 }
            ]);

            var lefthandProducts = products.Where(p => m_assemblyInfo[p.ID].CounterclockwiseOperations.Any());
            var righthandProducts = products.Except(lefthandProducts);

            if (righthandProducts.Any())
            {
                BuildRighthandOutputs(righthandProducts);
            }
            if (lefthandProducts.Any())
            {
                BuildLefthandOutputs(lefthandProducts);
            }
        }

        private void BuildRighthandOutputs(IEnumerable<Molecule> products)
        {
            int index = 0;
            HexRotation rotationOffset = HexRotation.R0;
            foreach (var product in products)
            {
                // Offset so the the center of the molecule is at (0, 0) (need to do this before rotating it)
                var transform = new Transform2D(-new Vector2(1, 1), HexRotation.R0);

                var rotation = m_assemblyInfo[product.ID].GetOutputRotation() + rotationOffset;

                // Rotate the glyph and move it to the correct location
                var productCenter = new Vector2(3 + index * 3, -3 - index * 3);
                transform = new Transform2D(productCenter, rotation).Apply(transform);
                new Product(this, transform.Position, transform.Rotation, product);

                m_outputLocationsById[product.ID] = index;
                if (index > 0)
                {
                    m_rightOutputArms.Add(new Arm(this, productCenter + new Vector2(0, 3), HexRotation.R180, ArmType.Arm1, 3));
                }

                rotationOffset = rotationOffset.Rotate60Counterclockwise();
                index++;
            }
        }

        private void BuildLefthandOutputs(IEnumerable<Molecule> products)
        {
            int index = 0;
            HexRotation rotationOffset = HexRotation.R0;
            foreach (var product in products)
            {
                // Offset so the the center of the molecule is at (0, 0) (need to do this before rotating it)
                var transform = new Transform2D(-new Vector2(1, 1), HexRotation.R0);

                var rotation = m_assemblyInfo[product.ID].GetOutputRotation() + rotationOffset;

                // Rotate the glyph and move it to the correct location
                var productCenter = new Vector2(-1, -3 - index * 3);
                transform = new Transform2D(productCenter, rotation).Apply(transform);
                new Product(this, transform.Position, transform.Rotation, product);

                m_outputLocationsById[product.ID] = index;
                if (index > 0)
                {
                    m_leftOutputArms.Add(new Arm(this, productCenter + new Vector2(-3, 3), HexRotation.R0, ArmType.Arm1, 3));
                }

                rotationOffset = rotationOffset.Rotate60Clockwise();
                index++;
            }
        }

        public override IEnumerable<Element> GetProductElementOrder(Molecule product)
        {
            var assemblyInfo = m_assemblyInfo[product.ID];
            if (assemblyInfo.CenterAtom != null)
            {
                return assemblyInfo.GetElementsInBuildOrder();
            }
            else
            {
                throw new NotImplementedException("Molecules without a center atom are yet supported");
            }
        }

        public override void AddAtom(Element element, int productID)
        {
            m_currentProductAssemblyInfo = m_assemblyInfo[productID];
            m_assembleCoroutine.Next();
        }

        private IEnumerable<object> Assemble()
        {
            // Move the center atom to the RHS of the bonder
            Writer.WriteGrabResetAction(m_horizontalArm, Instruction.MoveNegative);
            Writer.Write(m_assemblyArm, Instruction.Grab);

            var ops = m_currentProductAssemblyInfo.CenterOperations;
            if (!ops.Any())
            {
                // TODO: Move atom to output
                // TODO: For a monoatomic atom we can probably put the product right next to these arms
                yield return null;
                yield break;
            }

            // Bond radial atoms to the center atom
            foreach (var op in ops)
            {
                switch (op.Type)
                {
                    case ProductAssemblyInfo.OperationType.RotateClockwise:
                        Writer.Write(m_assemblyArm, Instruction.PivotClockwise);
                        break;
                    case ProductAssemblyInfo.OperationType.RotateCounterclockwise:
                        Writer.Write(m_assemblyArm, Instruction.PivotCounterclockwise);
                        break;
                    case ProductAssemblyInfo.OperationType.GrabAtom:
                        yield return null;
                        break;
                    case ProductAssemblyInfo.OperationType.Bond:
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid operation {op.Type} while bonding atoms to center atom.");
                }
            }

            var clockwiseOps = m_currentProductAssemblyInfo.ClockwiseOperations;
            var counterclockwiseOps = m_currentProductAssemblyInfo.CounterclockwiseOperations;
            if (!clockwiseOps.Any() && !counterclockwiseOps.Any())
            {
                // Move the other arm out the way to avoid a collision
                Writer.Write(m_horizontalArm, [Instruction.MoveNegative, Instruction.MovePositive], updateTime: false);

                // Move to the output area without creating any extra bonds
                Writer.Write(m_assemblyArm, [Instruction.RotateClockwise, Instruction.MovePositive, Instruction.Reset]);
                Writer.AdjustTime(-1);
                MoveProductToOutput(m_currentProductAssemblyInfo.Product, m_rightOutputArms, Instruction.RotateCounterclockwise);
                yield return null;
                yield break;
            }

            // Move the molecule down one row so we can weld radial atoms together or to new atoms
            Writer.Write(m_assemblyArm, Instruction.MovePositive);

            foreach (var op in clockwiseOps)
            {
                switch (op.Type)
                {
                    case ProductAssemblyInfo.OperationType.RotateClockwise:
                        Writer.Write(m_assemblyArm, Instruction.PivotClockwise);
                        break;
                    case ProductAssemblyInfo.OperationType.RotateCounterclockwise:
                        Writer.Write(m_assemblyArm, Instruction.PivotCounterclockwise);
                        break;
                    case ProductAssemblyInfo.OperationType.GrabAtom:
                        yield return null;
                        break;
                    case ProductAssemblyInfo.OperationType.Bond:
                        break;
                    case ProductAssemblyInfo.OperationType.MoveAwayFromBonder:
                        Writer.Write(m_assemblyArm, Instruction.MovePositive);
                        break;
                    case ProductAssemblyInfo.OperationType.MoveTowardBonder:
                        Writer.Write(m_assemblyArm, Instruction.MoveNegative);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid operation {op.Type} while bonding atoms clockwise.");
                }
            }

            if (!counterclockwiseOps.Any())
            {
                Writer.Write(m_assemblyArm, [Instruction.RotateClockwise, Instruction.Reset]);
                Writer.AdjustTime(-1);
                MoveProductToOutput(m_currentProductAssemblyInfo.Product, m_rightOutputArms, Instruction.RotateCounterclockwise);
                yield return null;
                yield break;
            }

            // Rotate the molecule so we can weld atoms to the top RHS
            Writer.Write(m_assemblyArm, Instruction.RotateCounterclockwise);

            foreach (var op in counterclockwiseOps)
            {
                switch (op.Type)
                {
                    case ProductAssemblyInfo.OperationType.RotateClockwise:
                        Writer.Write(m_assemblyArm, Instruction.PivotClockwise);
                        break;
                    case ProductAssemblyInfo.OperationType.RotateCounterclockwise:
                        Writer.Write(m_assemblyArm, Instruction.PivotCounterclockwise);
                        break;
                    case ProductAssemblyInfo.OperationType.GrabAtom:
                        yield return null;
                        Writer.WriteGrabResetAction(m_horizontalArm, Instruction.MovePositive);
                        break;
                    case ProductAssemblyInfo.OperationType.Bond:
                        break;
                    case ProductAssemblyInfo.OperationType.MoveAwayFromBonder:
                        Writer.Write(m_assemblyArm, Instruction.MovePositive);
                        break;
                    case ProductAssemblyInfo.OperationType.MoveTowardBonder:
                        Writer.Write(m_assemblyArm, Instruction.MoveNegative);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid operation {op.Type} while bonding atoms counterclockwise.");
                }
            }

            Writer.Write(m_assemblyArm, [Instruction.RotateCounterclockwise, Instruction.Reset]);
            Writer.AdjustTime(-1);
            MoveProductToOutput(m_currentProductAssemblyInfo.Product, m_leftOutputArms, Instruction.RotateClockwise);
            yield return null;
        }

        private void MoveProductToOutput(Molecule product, List<Arm> outputArms, Instruction armInstruction)
        {
            int outputLocation = m_outputLocationsById[product.ID];
            if (outputLocation > 0)
            {
                for (int i = 0; i < outputLocation; i++)
                {
                    Writer.WriteGrabResetAction(outputArms[i], armInstruction);
                }
            }
        }
    }
}
