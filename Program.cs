using System;
using System.Windows.Forms;
using AllInOne_TVBox_Suite.UI;

namespace AllInOne_TVBox_Suite
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainDashboardForm());
        }
    }
}
