using System;
using Microsoft.VisualStudio.Shell;

namespace CoApp.VisualStudio.VsCore
{
    public static class ExceptionHelper
    {
        private const string LogEntrySource = "CoApp.VisualStudio Package Manager";

        public static void WriteToActivityLog(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            exception = ExceptionUtility.Unwrap(exception);

            ActivityLog.LogError(LogEntrySource, exception.Message + exception.StackTrace);
        }
    }
}