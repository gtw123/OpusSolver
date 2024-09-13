﻿using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    public class GridState
    {
        private Dictionary<Vector2, Element?> m_atoms = new();
        private Dictionary<Vector2, Arm> m_arms = new();
        private Dictionary<Vector2, GlyphType> m_glyphs = new();

        public void RegisterAtom(Vector2 position, Element? element, GameObject relativeToObj)
        {
            position = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            m_atoms[position] = element;
        }

        public void RegisterAtoms(AtomCollection atomCollection)
        {
            RegisterMoleculeElements(atomCollection, true);
        }

        public void UnregisterAtoms(AtomCollection atomCollection)
        {
            RegisterMoleculeElements(atomCollection, false);
        }

        private void RegisterMoleculeElements(AtomCollection atomCollection, bool register)
        {
            foreach (var (atom, pos) in atomCollection.GetTransformedAtomPositions())
            {
                m_atoms[pos] = register ? atom.Element : null;
            }
        }

        /// <summary>
        /// Checks if a molecule positioned at a certain position/rotation in the grid will collide with any
        /// other atoms registered in the grid. Does not currently check if the molecule will collide while
        /// in the process of rotating - only whether it will overlap with anything else once it has rotated.
        /// </summary>
        public bool WillAtomsCollideWhileRotating(AtomCollection atomCollection, Vector2 rotationPoint, HexRotation rotation, GameObject relativeToObj)
        {
            var objTransform = relativeToObj?.GetWorldTransform() ?? new Transform2D();
            var transform = new Transform2D().RotateAbout(objTransform.Apply(rotationPoint), rotation);

            foreach (var (_, pos) in atomCollection.GetTransformedAtomPositions())
            {
                var worldPos = transform.Apply(pos);
                if (GetAtom(worldPos) != null || GetArm(worldPos) != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void RegisterArm(Vector2 position, Arm arm, GameObject relativeToObj = null)
        {
            position = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            m_arms[position] = arm;
        }

        public void RegisterGlyph(Vector2 position, GlyphType glyphType, GameObject relativeToObj = null)
        {
            position = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            m_glyphs[position] = glyphType;
        }

        public Element? GetAtom(Vector2 position)
        {
            return m_atoms.TryGetValue(position, out var element) ? element : null;
        }

        public Arm GetArm(Vector2 position)
        {
            return m_arms.TryGetValue(position, out var arm) ? arm : null;
        }

        public GlyphType? GetGlyph(Vector2 position)
        {
            return m_glyphs.TryGetValue(position, out var glyphType) ? glyphType : null;
        }
    }
}
