using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace Opus
{
    public class Molecule
    {
        public MoleculeType Type { get; private set; }

        private List<Atom> m_atoms;
        public IEnumerable<Atom> Atoms
        {
            get { return m_atoms; }
        }

        public int ID { get; set; }

        public Vector2 Origin { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        public int DiagonalLength { get; private set; }
        public int Rotation { get; private set; }
        public bool HasRepeats { get; private set; }
        public bool HasTriplex { get; private set; }

        public Molecule(MoleculeType type, IEnumerable<Atom> atoms)
        {
            Type = type;
            m_atoms = atoms.ToList();
            HasRepeats = atoms.Any(atom => atom.Element == Element.Repeat);
            HasTriplex = atoms.Any(a => a.Bonds.Any(b => b == BondType.Triplex));

            AdjustBounds();
        }

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

            Origin = Origin.Subtract(offset);
        }

        public Atom GetAtom(Vector2 position)
        {
            return m_atoms.SingleOrDefault(a => a.Position == position);
        }

        public Atom GetAdjacentAtom(Vector2 position, int direction)
        {
            return GetAtom(position.OffsetInDirection(direction, 1));
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

        public void Rotate60Counterclockwise()
        {
            foreach (var atom in m_atoms)
            {
                atom.Position = atom.Position.Rotate60Counterclockwise();
                atom.Bonds.Insert(0, atom.Bonds[Direction.Count - 1]);
                atom.Bonds.RemoveAt(Direction.Count);
            }

            Origin = Origin.Rotate60Counterclockwise();
            Rotation = DirectionUtil.Rotate60Counterclockwise(Rotation);

            AdjustBounds();
        }

        public void Rotate60Clockwise()
        {
            foreach (var atom in m_atoms)
            {
                atom.Position = atom.Position.Rotate60Clockwise();
                atom.Bonds.Add(atom.Bonds[0]);
                atom.Bonds.RemoveAt(0);
            }

            Origin = Origin.Rotate60Clockwise();
            Rotation = DirectionUtil.Rotate60Clockwise(Rotation);

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
                        row1.Append(GetBondString(atom, Direction.NW));
                        row1.Append(GetBondString(atom, Direction.NE));

                        row2.Append(GetBondString(atom, Direction.W));
                        row2.Append(atom?.ToString() ?? " ");
                        row2.Append(GetBondString(atom, Direction.E).Substring(0, 1));

                        row3.Append(GetBondString(atom, Direction.SW));
                        row3.Append(GetBondString(atom, Direction.SE));
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

        private static string GetBondString(Atom atom, int direction)
        {
            var bondType = atom.Bonds[direction];
            if (bondType == BondType.None)
            {
                return "  ";
            }

            switch (direction)
            {
                case Direction.W:
                case Direction.E:
                    return bondType == BondType.Single ? "--" : "==";
                case Direction.NW:
                    return bondType == BondType.Single ? @" \" : @"\\";
                case Direction.NE:
                    return bondType == BondType.Single ? " /" : "//";
                case Direction.SW:
                    return bondType == BondType.Single ? "/ " : "//";
                case Direction.SE:
                    return bondType == BondType.Single ? @"\ " : @"\\";
                default:
                    throw new ArgumentOutOfRangeException("direction", direction, Invariant($"Invalid direction."));
            }
        }
    }
}
