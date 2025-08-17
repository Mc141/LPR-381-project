using System;
using System.Windows.Forms;

namespace LPR381_Assignment
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new TabbedMainForm());


        }
    }
}
