using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CoreUtils
{
    public class CrashDumpWriter
    {
        public static void DumpException(Exception e, string title, string email)
        {
            try
            {
                string desktopPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Desktop);

                string crashDmpFilename = Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                    "PureMp3.Diagnostics.txt");

                bool fileExists = File.Exists(crashDmpFilename);

                using (TextWriter tw = new StreamWriter(crashDmpFilename, true))
                {
                    if (!fileExists)
                    {
                        tw.WriteLine("---------------------------------------------------------------------------------");
                        tw.WriteLine(title);
                        tw.WriteLine("---------------------------------------------------------------------------------");
                        tw.WriteLine("Crashreport: Please send this file to " + email);
                    }

                    tw.WriteLine("---------------------------------------------------------------------------------");
                    tw.WriteLine(e.GetType());
                    tw.WriteLine(e.Message);
                    tw.WriteLine(e.StackTrace);
                }

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }

            }
            catch (Exception finalException)
            {
                Console.WriteLine(finalException);
            }
        }
    }
}
