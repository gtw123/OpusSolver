using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.FormattableString;

namespace OpusSolver
{
    public class Molecule
    {
        public MoleculeType Type { get; private set; }

        private List<Atom> m_atoms;
        public IEnumerable<Atom> Atoms
        {
            get { return m_atoms; }
        }

        public int ID { get; private set; }

        /// <summary>
        /// Transform from the original positions/rotations of the atoms to their current positions/rotations.
        /// This is not important for generating a solution, but is important when placing a product/reagent glyph.
        /// </summary>
        public Transform2D GlyphTransform { get; private set; } = new Transform2D();

        public int Height { get; private set; }
        public int Width { get; private set; }
        public int DiagonalLength { get; private set; }

        public bool HasRepeats { get; private set; }
        public bool HasTriplex { get; private set; }

        public bool IsLinear => Height == 1;
        public int Size => Math.Max(Height, Math.Max(Width, DiagonalLength));

        public Molecule(MoleculeType type, IEnumerable<Atom> atoms, int id)
        {
            Type = type;
            m_atoms = atoms.ToList();
            ID = id;

            HasRepeats = atoms.Any(atom => atom.Element == Element.Repeat);
            HasTriplex = atoms.Any(a => a.Bonds.Values.Any(b => b == BondType.Triplex));

            AdjustBounds();

            NormalizeOrientation();
        }

        /// <summary>
        /// Recalculates the bounds of this molecule and moves atoms if necessary so that the minimum X/Y coordinates are 0 and 0.
        /// Note that there may not necessarily be an atom at (0, 0), depending on the geometry of the molecule.
        /// Note that the GlyphTransform.Position may still be located elsewhere, but that's generally only relevant when placing glyphs.
        /// </summary>
        private void AdjustBounds()
        {
            int minX = m_atoms.Min(a => a.Position.X);
            int minY = m_atoms.Min(a => a.Position.Y);
            int maxX = m_atoms.Max(a => a.Position.X);
            int maxY = m_atoms.Max(a => a.Position.Y);

            Width = maxX - minX + 1;
            Height = maxY - minY + 1;
            DiagonalLength = m_atoms.Max(a => a.Position.X + a.Position.Y) - m_atoms.Min(a => a.Position.X + a.Position.Y) + 1;

            // Translate all atoms so that (minX, minY) is at (0, 0)
            var offset = new Vector2(minX, minY);
            foreach (var atom in Atoms)
            {
                atom.Position = atom.Position.Subtract(offset);
            }

            GlyphTransform.Position -= offset;
        }

        /// <summary>
        /// Rotates the molecule so that its shortest dimension is Y (i.e. height).
        /// </summary>
        private void NormalizeOrientation()
        {
            if (HasRepeats)
            {
                // Can't rotate molecules with repeats
                return;
            }

            if (Height > Width || Height > DiagonalLength)
            {
                Rotate60Clockwise();

                if (Height > Width || Height > DiagonalLength)
                {
                    Rotate60Clockwise();
                }
            }
        }

        public Atom GetAtom(Vector2 position)
        {
            return m_atoms.SingleOrDefault(a => a.Position == position);
        }

        public Atom GetAdjacentAtom(Vector2 position, HexRotation direction)
        {
            return GetAtom(position.OffsetInDirection(direction, 1));
        }

        /// <summary>
        /// Returns whether atom1 is next to atom2 on the hex grid, ignoring bonds.
        /// </summary>
        public bool AreAtomsAdjacent(Atom atom1, Atom atom2)
        {
            var offset = atom2.Position - atom1.Position;
            return (offset.X == 0 && Math.Abs(offset.Y) == 1)
                || (offset.Y == 0 && Math.Abs(offset.X) == 1)
                || (offset.X == -offset.Y && Math.Abs(offset.X) == 1);
        }

        public IEnumerable<Atom> GetRow(int row)
        {
            return Enumerable.Range(0, Width).Select(x => GetAtom(new Vector2(x, row))).Where(a => a != null);
        }

        public IEnumerable<Atom> GetColumn(int column)
        {
            return Enumerable.Range(0, Height).Select(y => GetAtom(new Vector2(column, y))).Where(a => a != null);
        }

        public IEnumerable<Atom> GetAtomsInInputOrder()
        {
            return Atoms.OrderByDescending(a => a.Position.Y).ThenByDescending(a => a.Position.X);
        }

        public void RotateBy(HexRotation rotation)
        {
            foreach (var atom in m_atoms)
            {
                atom.Position = atom.Position.RotateBy(rotation);

                var oldBonds = new Dictionary<HexRotation, BondType>(atom.Bonds);
                foreach (var (dir, type) in oldBonds)
                {
                    atom.Bonds[dir + rotation] = type;
                }
            }

            GlyphTransform = new Transform2D(new Vector2(), rotation).Apply(GlyphTransform);
            AdjustBounds();
        }

        public void Rotate60Counterclockwise() => RotateBy(HexRotation.R60);
        public void Rotate60Clockwise() => RotateBy(HexRotation.R300);
        public void Rotate180() => RotateBy(HexRotation.R180);

        /// <summary>
        /// Expands out the "repeat" atom (if any) of the molecule by copying the other atoms so there are a
        /// total of 6 copies. This brute force approach is is necessary because the solver isn't currently smart
        /// enough to bond together the repeating parts in the output area.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void ExpandRepeats()
        {
            var repeatAtoms = m_atoms.Where(atom => atom.Element == Element.Repeat);
            if (repeatAtoms.Count() == 0)
            {
                return;
            }
            else if (repeatAtoms.Count() > 1)
            {
                throw new InvalidOperationException($"Molecule has more than one repeating atom.");
            }

            var repeatAtom = repeatAtoms.First();
            var leftmostAtom = GetRow(repeatAtom.Position.Y).First();
            int width = repeatAtom.Position.X - leftmostAtom.Position.X;
            if (width == 0)
            {
                throw new InvalidOperationException($"Molecule has no atom to the left of the repeating atom.");
            }

            /*
                Example:

                D   C           D   C   D   C   D   C   D   C   D   C   D   C
                 \ / \           \ / \   \ / \   \ / \   \ / \   \ / \   \ / \
                  A - B - R  ->   A - B - A - B - A - B - A - B - A - B - A - B - A
                   \ /             \ /     \ /     \ /     \ /     \ /     \ /
                    E               E       E       E       E       E       E
             */

            var atomsToCopy = m_atoms.Where(atom => atom != repeatAtom).ToList();
            const int RepeatCount = 6;
            for (int repeat = 1; repeat < RepeatCount; repeat++)
            {
                Vector2 offset = new Vector2(repeat * width, 0);
                foreach (var atom in atomsToCopy)
                {
                    var newAtom = new Atom(atom.Element, atom.Bonds, atom.Position + offset);
                    m_atoms.Add(newAtom);

                    if (atom == leftmostAtom)
                    {
                        // Bond the atom to the rest of the molecule by copying the bonds from the repeat atom
                        foreach (var dir in HexRotation.All)
                        {
                            var bondType = repeatAtom.Bonds[dir];
                            if (bondType != BondType.None)
                            {
                                newAtom.Bonds[dir] = bondType;
                            }
                        }
                    }
                }
            }

            // Move the original repeat atom to the end of the molecule and set its element to the same as the
            // left-most atom on the same row. Otherwise, if there are otherwise-unconnected atoms bonded to the
            // top/bottom of the repeat atom, the solver won't be able to construct the product properly.
            repeatAtom.Position = repeatAtom.Position + new Vector2((RepeatCount- 1) * width, 0);
            repeatAtom.Element = leftmostAtom.Element;

            AdjustBounds();
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            for (int y = Height - 1; y >= 0; y--)
            {
                string indent = new String(' ', y * 2);
                var row1 = new StringBuilder(indent);
                var row2 = new StringBuilder(indent);
                var row3 = new StringBuilder(indent);

                for (int x = 0; x < Width; x++)
                {
                    var atom = GetAtom(new Vector2(x, y));
                    if (atom == null)
                    {
                        row1.Append("    ");
                        row2.Append("    ");
                    }
                    else
                    {
                        row1.Append(GetBondString(atom, HexRotation.R120));
                        row1.Append(GetBondString(atom, HexRotation.R60));

                        row2.Append(GetBondString(atom, HexRotation.R180));
                        row2.Append(atom?.ToString() ?? " ");
                        row2.Append(GetBondString(atom, HexRotation.R0).Substring(0, 1));

                        row3.Append(GetBondString(atom, HexRotation.R240));
                        row3.Append(GetBondString(atom, HexRotation.R300));
                    }
                }

                str.AppendLine(row1.ToString());
                str.AppendLine(row2.ToString());
                if (y == 0)
                {
                    str.AppendLine(row3.ToString());
                }
            }

            return str.ToString();
        }

        private static string GetBondString(Atom atom, HexRotation direction)
        {
            var bondType = atom.Bonds[direction];
            if (bondType == BondType.None)
            {
                return "  ";
            }

            return direction.IntValue switch
            {
                0 or 3 => bondType == BondType.Single ? "--" : "==",
                1 => bondType == BondType.Single ? " /" : "//",
                2 => bondType == BondType.Single ? @" \" : @"\\",
                4 => bondType == BondType.Single ? "/ " : "//",
                5 => bondType == BondType.Single ? @"\ " : @"\\",
                _ => throw new ArgumentOutOfRangeException("direction", direction, Invariant($"Invalid direction."))
            };
        }
    }
}
