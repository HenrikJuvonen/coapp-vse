using System;
using Microsoft.VisualStudio.Shell;
using CoApp.Toolkit.Extensions;

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

            exception = exception.Unwrap();

            ActivityLog.LogError(LogEntrySource, exception.Message + exception.StackTrace);
        }
    }
}