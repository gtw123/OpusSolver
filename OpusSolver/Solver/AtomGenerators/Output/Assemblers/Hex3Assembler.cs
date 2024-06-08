using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers
{
    /// <summary>
    /// Assembles molecules that fit within a hexagon of diagonal length 3 and have a central atom, e.g.
    ///   O - O
    ///  / \ / \
    /// O - O - O
    ///  \ / \ /
    ///   O - O
    /// </summary>
    public class Hex3Assembler : MoleculeAssembler
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(Hex3Assembler));

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

        private class ProductAssemblyInfo
        {
            public Molecule Product { get; private set; }
            public Atom CenterAtom { get; private set; }

            public IEnumerable<Operation> CenterOperations => m_centerOperations;
            public IEnumerable<Operation> ClockwiseOperations => m_clockwiseOperations;
            public IEnumerable<Operation> CounterclockwiseOperations => m_counterclockwiseOperations;
            public IEnumerable<Operation> AllOperations => CenterOperations.Concat(ClockwiseOperations).Concat(CounterclockwiseOperations);

            private List<Operation> m_centerOperations;
            private List<Operation> m_clockwiseOperations;
            private List<Operation> m_counterclockwiseOperations;

            private bool m_needAvoidBondBeforeCounterclockwiseOperations = false;
            private HexRotation m_currentDirection = HexRotation.R0;
            private readonly HexRotationDictionary<Atom> m_grabbedAtoms = new();
            private readonly HashSet<HexRotation> m_constructedClockwiseBonds = new(); // Whether a bond has been constructed in a clockwise direction from the atom in the specified radial direction

            public ProductAssemblyInfo(Molecule product)
            {
                Product = product;
                CenterAtom = GetCenterAtom(product);
                if (CenterAtom == null)
                {
                    throw new ArgumentException($"{nameof(Hex3Assembler)} can't handle molecules with the center atom missing.");
                }

                m_centerOperations = GenerateCenterOperations().ToList();
                m_clockwiseOperations = GenerateClockwiseOperations().ToList();
                m_counterclockwiseOperations = GenerateCounterclockwiseOperations().ToList();

                // TODO: Can we avoid this in some cases by pivoting before rotating to LH bonder?
                if (m_needAvoidBondBeforeCounterclockwiseOperations && m_counterclockwiseOperations.Any())
                {
                    var rot = m_centerOperations.Concat(m_clockwiseOperations).Last().FinalRotation;
                    m_clockwiseOperations.Add(new Operation { Type = OperationType.MoveAwayFromBonder, FinalRotation = rot });
                }
            }

            public IEnumerable<Element> GetElementsInBuildOrder()
            {
                return new[] { CenterAtom.Element }.Concat(AllOperations.Where(op => op.Type == OperationType.GrabAtom).Select(op => op.Atom.Element));
            }

            public HexRotation GetOutputRotation()
            {
                if (!AllOperations.Any())
                {
                    // This occurs when the molecule has only 1 atom
                    return HexRotation.R0;
                }

                var rot = AllOperations.Last().FinalRotation;
                if (CounterclockwiseOperations.Any())
                {
                    return HexRotation.R120 - rot;
                }
                else if (ClockwiseOperations.Any())
                {
                    return HexRotation.R60 - rot;
                }
                else
                {
                    return HexRotation.R120 - rot;
                }
            }

            private IEnumerable<Operation> GenerateCenterOperations()
            {
                // Get the atoms that need to be bonded to the center atom
                var atoms = Product.GetAdjacentAtoms(CenterAtom.Position, (dir, atom) => atom.Bonds[dir + HexRotation.R180] != BondType.None);

                // After we've bonded the radial atoms to the center atom, we'll move the molecule down one cell,
                // which means the top two atoms are on top of a bonder. This could create an unwanted bond,
                // so we try to choose the last radial atom so that we end up with at least one gap in the
                // top two cells.
                HexRotation lastAtomDir;
                if (atoms.Count != HexRotation.Count)
                {
                    lastAtomDir = atoms.Keys.FirstOrDefault(dir => !atoms.ContainsKey(dir.Rotate60Clockwise()));
                }
                else
                {
                    // There are no empty gaps in the radial atoms, so instead try to make sure the top two
                    // atoms actually do need a bond, to avoid creating an unwanted bond. If there is no such
                    // atom then FirstOrDefault will return default(HexRotation) which is fine because in this
                    // case we won't even bother moving the molecule down one cell (it'll already be complete).
                    lastAtomDir = atoms.Keys.FirstOrDefault(dir => atoms[dir.Rotate60Clockwise()].Bonds[dir.Rotate180()] != BondType.None);
                }

                // Enumerate the atoms so that lastAtomDir is last
                var startDir = lastAtomDir.Rotate60Clockwise();
                m_currentDirection = atoms.EnumerateClockwise(startFrom: startDir).FirstOrDefault().Key;
                foreach (var (dir, atom) in atoms.EnumerateClockwise(startFrom: startDir))
                {
                    var (numRotations, rotationDir) = CalculateRotation(m_currentDirection, dir);
                    foreach (var op in CreateRotationOperations(numRotations, rotationDir, false, false))
                    {
                        yield return op;
                    }

                    m_grabbedAtoms[dir] = atom;
                    yield return new Operation { Type = OperationType.GrabAtom, FinalRotation = m_currentDirection, Atom = atom };
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
                var atoms = Product.GetAdjacentAtoms(CenterAtom.Position, (dir, atom) => atom.Bonds[dir - HexRotation.R120] != BondType.None);

                m_currentDirection = m_currentDirection.Rotate60Clockwise();

                // Start with the atom next to the first one we've already grabbed
                var startDir = m_grabbedAtoms.Keys.FirstOrDefault().Rotate60Counterclockwise();
                foreach (var (dir, atom) in atoms.EnumerateCounterclockwise(startFrom: startDir))
                {
                    // We can only do anything if the next atom has already been grabbed
                    if (m_grabbedAtoms.ContainsKey(dir.Rotate60Clockwise()))
                    {
                        var (numRotations, rotationDir) = CalculateRotation(m_currentDirection, dir);

                        // Check if rotating without moving would create any unwanted bonds
                        var newDir = m_currentDirection;
                        bool moveWhileRotating = false;
                        for (int i = 0; i < numRotations; i++)
                        {
                            newDir += rotationDir;
                            if (m_grabbedAtoms.ContainsKey(newDir) && m_grabbedAtoms.ContainsKey(newDir.Rotate60Clockwise()))
                            {
                                if (!atoms.ContainsKey(newDir))
                                {
                                    moveWhileRotating = true;
                                    break;
                                }
                            }
                        }

                        foreach (var op in CreateRotationOperations(numRotations, rotationDir, moveWhileRotating, moveWhileRotating))
                        {
                            yield return op;
                        }

                        m_constructedClockwiseBonds.Add(dir);

                        if (!m_grabbedAtoms.ContainsKey(dir))
                        {
                            m_grabbedAtoms[dir] = atom;
                            yield return new Operation { Type = OperationType.GrabAtom, FinalRotation = m_currentDirection, Atom = atom };
                        }
                        else
                        {
                            yield return new Operation { Type = OperationType.Bond, FinalRotation = m_currentDirection, Atom = atom };
                        }
                    }
                }
            }

            private IEnumerable<Operation> GenerateCounterclockwiseOperations()
            {
                // Get the atoms that need to be bonded to the next atom in a counter-clockwise direction
                var atoms = Product.GetAdjacentAtoms(CenterAtom.Position, (dir, atom) => atom.Bonds[dir + HexRotation.R120] != BondType.None);

                m_currentDirection -= HexRotation.R120;

                if (m_grabbedAtoms.ContainsKey(m_currentDirection) && m_grabbedAtoms.ContainsKey(m_currentDirection.Rotate60Counterclockwise()))
                {
                    if (!atoms.ContainsKey(m_currentDirection))
                    {
                        m_needAvoidBondBeforeCounterclockwiseOperations = true;
                    }
                }

                // Start with the atom next to the first one we've already grabbed
                var startDir = m_grabbedAtoms.Keys.FirstOrDefault().Rotate60Clockwise();
                bool isFirstOp = true;
                foreach (var (dir, atom) in atoms.EnumerateClockwise(startFrom: startDir))
                {
                    if (!m_constructedClockwiseBonds.Contains(dir.Rotate60Counterclockwise()))
                    {
                        var (numRotations, rotationDir) = CalculateRotation(m_currentDirection, dir);

                        bool moveBefore = false;
                        bool moveAfter = false;
                        if (isFirstOp && m_needAvoidBondBeforeCounterclockwiseOperations)
                        {
                            // In this case we don't need to write the "move away" op, only the "move toward"
                            moveAfter = true;
                        }
                        else
                        {
                            // Check if rotating without moving would create any unwanted bonds
                            var newDir = m_currentDirection;
                            for (int i = 0; i < numRotations; i++)
                            {
                                newDir += rotationDir;
                                if (m_grabbedAtoms.ContainsKey(newDir) && m_grabbedAtoms.ContainsKey(newDir.Rotate60Counterclockwise()))
                                {
                                    if (!atoms.ContainsKey(newDir))
                                    {
                                        moveBefore = true;
                                        moveAfter = true;
                                        break;
                                    }
                                }
                            }
                        }

                        foreach (var op in CreateRotationOperations(numRotations, rotationDir, moveBefore, moveAfter))
                        {
                            yield return op;
                        }

                        m_constructedClockwiseBonds.Add(dir.Rotate60Counterclockwise());

                        if (!m_grabbedAtoms.ContainsKey(dir))
                        {
                            m_grabbedAtoms[dir] = atom;
                            yield return new Operation { Type = OperationType.GrabAtom, FinalRotation = m_currentDirection, Atom = atom };
                        }
                        else
                        {
                            yield return new Operation { Type = OperationType.Bond, FinalRotation = m_currentDirection, Atom = atom };
                        }

                        isFirstOp = false;
                    }
                }
            }

            private IEnumerable<Operation> CreateRotationOperations(int numRotations, HexRotation rotationDir, bool moveBeforeRotating, bool moveAfterRotatin)
            {
                if (moveBeforeRotating)
                {
                    yield return new Operation { Type = OperationType.MoveAwayFromBonder, FinalRotation = m_currentDirection };
                }

                for (int i = 0; i < numRotations; i++)
                {
                    m_currentDirection += rotationDir;
                    yield return new Operation
                    {
                        Type = (rotationDir == HexRotation.R60) ? OperationType.RotateClockwise : OperationType.RotateCounterclockwise,
                        FinalRotation = m_currentDirection
                    };
                }

                if (moveAfterRotatin)
                {
                    yield return new Operation { Type = OperationType.MoveTowardBonder, FinalRotation = m_currentDirection };
                }
            }
        }

        public override Vector2 OutputPosition => new Vector2();

        private readonly Dictionary<int, ProductAssemblyInfo> m_assemblyInfo;
        private readonly LoopingCoroutine<object> m_assembleCoroutine;
        
        private readonly Arm m_horizontalArm;
        private readonly Arm m_assemblyArm;
        private readonly List<Arm> m_rightOutputArms;
        private readonly List<Arm> m_leftOutputArms;
        private bool m_useSimplerRighthandOutputs = false;

        private ProductAssemblyInfo m_currentProductAssemblyInfo;

        private Dictionary<int, int> m_outputLocationsById = new();

        public Hex3Assembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer)
        {
            m_assemblyInfo = products.ToDictionary(p => p.ID, p => new ProductAssemblyInfo(p));

            m_assembleCoroutine = new LoopingCoroutine<object>(Assemble);

            var lefthandProducts = products.Where(p => m_assemblyInfo[p.ID].CounterclockwiseOperations.Any());
            var righthandProducts = products.Except(lefthandProducts);
            m_useSimplerRighthandOutputs = righthandProducts.All(p => !m_assemblyInfo[p.ID].ClockwiseOperations.Any());

            new Glyph(this, new Vector2(0, 0), HexRotation.R0, GlyphType.Bonding);
            if (lefthandProducts.Any())
            {
                new Glyph(this, new Vector2(-2, 0), HexRotation.R0, GlyphType.Bonding);
            }

            m_horizontalArm = new Arm(this, new Vector2(3, 0), HexRotation.R180, ArmType.Arm1, extension: 3);
            new Track(this, new Vector2(4, 0), HexRotation.R180, 2);

            m_assemblyArm = new Arm(this, new Vector2(1, -2), HexRotation.R60, ArmType.Arm1, extension: 2);
            new Track(this, new Vector2(1, -2), [
                new Track.Segment { Direction = HexRotation.R240, Length = 1 },
                new Track.Segment { Direction = HexRotation.R300, Length = 1 }
            ]);

            if (righthandProducts.Any())
            {
                var initialPosition = new Vector2(3, -3);
                // If we're only using center bonds, we can offset the outputs a little to save some cost/cycles
                if (m_useSimplerRighthandOutputs)
                {
                    initialPosition += new Vector2(0, 1);
                }

                m_rightOutputArms = AddOutputs(righthandProducts, initialPosition, new Vector2(3, -3), HexRotation.R60);
            }
            if (lefthandProducts.Any())
            {
                m_leftOutputArms = AddOutputs(lefthandProducts, new Vector2(-1, -3), new Vector2(0, -3), -HexRotation.R60);
            }
        }

        private List<Arm> AddOutputs(IEnumerable<Molecule> products, Vector2 initialPosition, Vector2 offset, HexRotation rotationOffset)
        {
            var arms = new List<Arm>();
            var currentRotationOffset = HexRotation.R0;

            // Build the products in reverse order so that the final product is closer to the assembly area (saves a few cycles)
            int index = 0;
            foreach (var product in products.Reverse())
            {
                // Offset so the the center of the molecule is at (0, 0) (need to do this before rotating it)
                var transform = new Transform2D(-m_assemblyInfo[product.ID].CenterAtom.Position, HexRotation.R0);

                var rotation = m_assemblyInfo[product.ID].GetOutputRotation() + currentRotationOffset;

                // Rotate the glyph and move it to the correct location
                var productCenter = initialPosition + index * offset;
                transform = new Transform2D(productCenter, rotation).Apply(transform);
                new Product(this, transform.Position, transform.Rotation, product);

                m_outputLocationsById[product.ID] = index;
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
            return m_assemblyInfo[product.ID].GetElementsInBuildOrder();
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

            // Bond radial atoms to the center atom
            var centerOps = m_currentProductAssemblyInfo.CenterOperations;
            foreach (var dummy in ProcessOperations(centerOps))
            {
                yield return dummy;
            }

            var clockwiseOps = m_currentProductAssemblyInfo.ClockwiseOperations;
            var counterclockwiseOps = m_currentProductAssemblyInfo.CounterclockwiseOperations;
            if (!clockwiseOps.Any() && !counterclockwiseOps.Any())
            {
                if (centerOps.Any())
                {
                    // Move the other arm out the way to avoid a collision
                    Writer.Write(m_horizontalArm, [Instruction.MoveNegative, Instruction.MovePositive], updateTime: false);
                }

                MoveProductToRighthandOutput(m_currentProductAssemblyInfo.Product, !m_useSimplerRighthandOutputs);
                yield return null;
                yield break;
            }

            // Move the molecule down one row so we can weld radial atoms in a clockwise direction
            Writer.Write(m_assemblyArm, Instruction.MovePositive);
            foreach (var dummy in ProcessOperations(clockwiseOps))
            {
                yield return dummy;
            }

            if (!counterclockwiseOps.Any())
            {
                MoveProductToRighthandOutput(m_currentProductAssemblyInfo.Product);
                yield return null;
                yield break;
            }

            // Rotate the molecule so we can weld radial atoms in a counterclockwise direction
            Writer.Write(m_assemblyArm, Instruction.RotateCounterclockwise);
            foreach (var dummy in ProcessOperations(counterclockwiseOps,
                afterGrab: () => Writer.WriteGrabResetAction(m_horizontalArm, Instruction.MovePositive)))
            {
                yield return dummy;
            }

            MoveProductToLefthandOutput(m_currentProductAssemblyInfo.Product);
            yield return null;
        }

        private IEnumerable<object> ProcessOperations(IEnumerable<Operation> centerOps, Action afterGrab = null)
        {
            foreach (var op in centerOps)
            {
                switch (op.Type)
                {
                    case OperationType.RotateClockwise:
                        Writer.Write(m_assemblyArm, Instruction.PivotClockwise);
                        break;
                    case OperationType.RotateCounterclockwise:
                        Writer.Write(m_assemblyArm, Instruction.PivotCounterclockwise);
                        break;
                    case OperationType.GrabAtom:
                        yield return null;
                        afterGrab?.Invoke();
                        break;
                    case OperationType.Bond:
                        break;
                    case OperationType.MoveAwayFromBonder:
                        Writer.Write(m_assemblyArm, Instruction.MovePositive);
                        break;
                    case OperationType.MoveTowardBonder:
                        Writer.Write(m_assemblyArm, Instruction.MoveNegative);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid operation {op.Type}.");
                }
            }
        }

        private void MoveProductToRighthandOutput(Molecule product, bool moveAfterRotate = false)
        {
            Instruction[] instructions = moveAfterRotate ? [Instruction.RotateClockwise, Instruction.MovePositive, Instruction.Reset]
                : [Instruction.RotateClockwise, Instruction.Reset];

            Writer.Write(m_assemblyArm, instructions);
            Writer.AdjustTime(-1);
            MoveProductToOutput(product, m_rightOutputArms, Instruction.RotateCounterclockwise);
        }

        private void MoveProductToLefthandOutput(Molecule product)
        {
            Writer.Write(m_assemblyArm, [Instruction.RotateCounterclockwise, Instruction.Reset]);
            Writer.AdjustTime(-1);
            MoveProductToOutput(product, m_leftOutputArms, Instruction.RotateClockwise);
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

            if (GetCenterAtom(product) == null)
            {
                sm_log.Debug($"Product {product.ID} has no center atom so can't be used by {nameof(Hex3Assembler)}.");
                return false;
            }

            return true;
        }

        private static Atom GetCenterAtom(Molecule product)
        {
            bool IsCentralAtom(Atom atom) => product.Atoms.All(atom2 => atom == atom2 || product.AreAtomsAdjacent(atom, atom2));
            int GetBondCount(Atom atom) => atom.Bonds.Values.Count(b => b != BondType.None);

            // Get the atom with the most bonds because that reduces the need to have multiple bonders in the assembler
            return product.Atoms.Where(atom => IsCentralAtom(atom)).OrderByDescending(atom => GetBondCount(atom)).FirstOrDefault();
        }
    }
}
