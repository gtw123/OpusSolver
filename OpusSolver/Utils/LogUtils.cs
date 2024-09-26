using OpusSolver.IO;
using OpusSolver.Solver;
using System;

namespace OpusSolver.Utils
{
    public static class LogUtils
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(LogUtils));

        public static void LogSolverException(string puzzleName, string puzzleFile, Exception e)
        {
            string exceptionDetail = e.Message;
            string message;
            switch (e)
            {
                case ParseException:
                    message = "Error loading puzzle file";
                    break;
                case SolverException:
                    message = "Error solving puzzle";
                    break;
                default:
                    message = "Internal error while solving puzzle";
                    exceptionDetail = e.ToString();
                    break;
            };

            if (puzzleName != null)
            {
                message += $" \"{puzzleName}\" from";
            }
            message += $" \"{puzzleFile}\": {exceptionDetail}";

            // Write a new line first because there may be progress dots on the current line
            Console.WriteLine();
            sm_log.Error(message);
        }
    }
}
