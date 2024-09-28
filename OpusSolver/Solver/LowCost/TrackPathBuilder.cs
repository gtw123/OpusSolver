using System.Linq;
using System.Collections.Generic;
using System;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Generates a track that passes through a specified list of points.
    /// </summary>
    public class TrackPathBuilder
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(TrackPathBuilder));

        private readonly Vector2[] m_points;
        private readonly Dictionary<int, int[]> m_adjacentPoints = new();

        public TrackPathBuilder(IEnumerable<Vector2> points)
        {
            m_points = points.Distinct().ToArray();

            for (int i = 0; i < m_points.Length; i++)
            {
                m_adjacentPoints[i] = Enumerable.Range(0, m_points.Length).Where(j => m_points[j].DistanceBetween(m_points[i]) == 1).ToArray();
            }
        }

        public (Vector2 StartPosition, IEnumerable<Track.Segment> Segments) CreateTrack()
        {
            var paths = FindPaths();
            if (!paths.Any())
            {
                throw new SolverException("Could not construct a track path that uses all points.");
            }

            var path = ChooseBestPath(paths);
            var segments = CreateSegments(path);
            return (m_points[path[0]], segments);
        }

        private List<int[]> FindPaths()
        {
            var currentPath = new List<int>();
            var usedPoints = Enumerable.Repeat(false, m_points.Length).ToArray();
            var foundPaths = new List<int[]>();

            for (int i = 0; i < m_points.Length; i++)
            {
                BuildPath(i);
            }

            void BuildPath(int index)
            {
                currentPath.Add(index);
                usedPoints[index] = true;

                bool foundAnyAdjacent = false;
                foreach (int adjacentPoint in m_adjacentPoints[index])
                {
                    if (!usedPoints[adjacentPoint])
                    {
                        foundAnyAdjacent = true;
                        BuildPath(adjacentPoint);
                    }
                }

                if (!foundAnyAdjacent && currentPath.Count == m_points.Length)
                {
                    foundPaths.Add(currentPath.ToArray());
                }

                currentPath.RemoveAt(currentPath.Count - 1);
                usedPoints[index] = false;
            }

            sm_log.Debug($"Found {foundPaths.Count} possible paths for {m_points.Length} points.");

            return foundPaths;
        }

        private int[] ChooseBestPath(List<int[]> paths)
        {
            // Prefer a closed path if there are any
            var closedPaths = paths.Where(p => m_adjacentPoints[p[0]].Contains(p[p.Length - 1]));
            var candidatePaths = closedPaths.Any() ? closedPaths.ToList() : paths;

            return candidatePaths.First();
        }

        private IEnumerable<Track.Segment> CreateSegments(int[] path)
        {
            var segments = new List<Track.Segment>();
            for (int i = 1; i < path.Length; i++)
            {
                var point = m_points[path[i]];
                var previousPoint = m_points[path[i - 1]];
                var dir = (point - previousPoint).ToRotation() ?? throw new InvalidOperationException($"Cannot create a straight track segment from {previousPoint} to {point}.");
                int length = point.DistanceBetween(previousPoint);
                segments.Add(new Track.Segment(dir, length));
            }

            return segments;
        }
    }
}