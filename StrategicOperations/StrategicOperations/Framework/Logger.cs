using System;
using System.IO;

namespace StrategicOperations.Framework
{
    //old logger, now unused and set to not compile
    internal class Logger
    {
        private static StreamWriter logStreamWriter;

        public Logger(string modDir, string fileName)
        {
            string filePath = Path.Combine(modDir, $"{fileName}.log");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            logStreamWriter = File.AppendText(filePath);
            logStreamWriter.AutoFlush = true;
        }

        public void LogMessage(string message)
        {
            if (ModInit.modSettings.enableLogging)
            {
                string ts = DateTime.Now.ToString("s.ff", System.Globalization.CultureInfo.InvariantCulture);
                logStreamWriter.WriteLine($"INFO: {ts} - {message}");
            }
        }

        public void LogTrace(string message)
        {
            if (ModInit.modSettings.enableTrace)
            {
                string ts = DateTime.Now.ToString("s.ff", System.Globalization.CultureInfo.InvariantCulture);
                logStreamWriter.WriteLine($"TRACE: {ts} - {message}");
            }
        }

        public void LogDev(string message)
        {
            if (ModInit.modSettings.DEVTEST_Logging)
            {
                string ts = DateTime.Now.ToString("s.ff", System.Globalization.CultureInfo.InvariantCulture);
                logStreamWriter.WriteLine($"TRACE: {ts} - {message}");
            }
        }

        public void LogError(string message)
        {
            string ts = DateTime.Now.ToString("s.ff", System.Globalization.CultureInfo.InvariantCulture);
            logStreamWriter.WriteLine($"ERROR: {ts} - {message}");
        }

        public static void LogException(Exception exception)
        {
            string ts = DateTime.Now.ToString("s.ff", System.Globalization.CultureInfo.InvariantCulture);
            logStreamWriter.WriteLine($"CRITICAL: {ts} - {exception}");
        }
    }
}