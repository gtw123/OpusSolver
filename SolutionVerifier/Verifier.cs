﻿using System;

namespace SolutionVerifier
{
    public class Verifier : IDisposable
    {
        private IntPtr m_verifier;

        public Verifier(string puzzleFile, string solutionFile)
        {
            m_verifier = NativeMethods.verifier_create(puzzleFile, solutionFile);
            CheckForError();
        }

        private int GetMetric(string metricName)
        {
            int metric = NativeMethods.verifier_evaluate_metric(m_verifier, metricName);
            CheckForError(includeCycleAndLocation: true);
            return metric;
        }

        public Metrics CalculateMetrics()
        {
            int overlap = GetMetric("overlap");
            if (overlap != 0)
            {
                throw new VerifierException("Solution contains overlapping parts");
            }

            return new Metrics
            {
                Cost = GetMetric("cost"),
                Cycles = GetMetric("cycles"),
                Area = GetMetric("area"),
                Instructions = GetMetric("instructions"),
            };
        }

        public void Dispose()
        {
            if (m_verifier != IntPtr.Zero)
            {
                NativeMethods.verifier_destroy(m_verifier);
                m_verifier = IntPtr.Zero;
            }
        }

        private void CheckForError(bool includeCycleAndLocation = false)
        {
            if (NativeMethods.GetVerifierError(m_verifier) != null)
            {
                throw new VerifierException(m_verifier, includeCycleAndLocation);
            }
        }
    }
}
