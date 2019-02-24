using System;
using System.IO;
using System.Windows.Forms;

namespace BridgePointsCounter
{
    static class Program
    {
        /// <summary>
        /// Główny punkt wejścia dla aplikacji.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                using (StreamWriter outfile = new StreamWriter(@"./error.txt"))
                {
                    outfile.Write("no elo \n");
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                //Write ex.Message to a file
                using (StreamWriter outfile = new StreamWriter(@"./error.txt"))
                {
                    outfile.Write(ex.Message.ToString());
                }
            }
            
        }
    }
}
