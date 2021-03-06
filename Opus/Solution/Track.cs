﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Opus.Solution
{
    /// <summary>
    /// Represents a track on the hex grid.
    /// </summary>
    public class Track : Mechanism
    {
        public struct Segment
        {
            public int Direction;
            public int Length;
        }

        private List<Vector2> m_path = new List<Vector2>();

        public IEnumerable<Vector2> Path => m_path;

        /// <summary>
        /// Creates a track with a straight path in the specified direction.
        /// </summary>
        public Track(GameObject parent, Vector2 position, int pathDirection, int pathLength)
            : this(parent, position, new[] { new Segment { Direction = pathDirection, Length = pathLength } })
        {
        }

        public Track(GameObject parent, Vector2 position, IEnumerable<Segment> segments)
            : base(parent, position, 0, MechanismType.Track)
        {
            var pos = new Vector2(0, 0);
            m_path.Add(pos);

            foreach (var segment in segments)
            {
                for (int i = 0; i < segment.Length; i++)
                {
                    pos = pos.OffsetInDirection(segment.Direction, 1);
                    m_path.Add(pos);
                }
            }
        }

        public IEnumerable<Vector2> GetAllPathCells()
        {
            var pos = GetWorldPosition();
            foreach (var cell in Path)
            {
                yield return pos.Add(cell);
            }
        }

        public Bounds GetBounds()
        {
            var allCells = GetAllPathCells();
            return new Bounds(
                new Vector2(allCells.Min(cell => cell.X), allCells.Min(cell => cell.Y)),
                new Vector2(allCells.Max(cell => cell.X), allCells.Max(cell => cell.Y)));
        }

        /// <summary>
        /// Removes cells that come before firstIndex or after lastIndex.
        /// </summary>
        public void TrimPath(int firstIndex, int lastIndex)
        {
            if (lastIndex < firstIndex)
            {
                throw new ArgumentOutOfRangeException("lastIndex", lastIndex, "lastIndex must be greater than or equal to firstIndex.");
            }

            if (lastIndex + 1 < m_path.Count())
            {
                m_path.RemoveRange(lastIndex + 1, m_path.Count() - (lastIndex + 1));
            }

            if (firstIndex > 0)
            {
                m_path.RemoveRange(0, firstIndex);

                // Make sure path still starts at (0, 0)
                var originOffset = m_path.First();
                for (int i = 0; i < m_path.Count(); i++)
                {
                    m_path[i] = m_path[i].Subtract(originOffset);
                }

                Position = Position.Add(originOffset);
            }
        }
    }
}
