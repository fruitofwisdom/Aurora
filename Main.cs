using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Aurora
{
    public class EntryPoint
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
        [STAThread]

        public static void Main(string[] args)
        {
            // If we are started with command line arguments, attach to or create a
            // console and run without a window. Otherwise, load the WPF application.
            if (args != null && args.Length > 0)
            {
                IntPtr window = GetForegroundWindow();
                int processId = 0;
                _ = GetWindowThreadProcessId(window, out processId);
                Process process = Process.GetProcessById(processId);
                if (process.ProcessName == "cmd")
                {
                    AttachConsole(process.Id);
                }
                else
                {
                    AllocConsole();
                }

                var consoleApp = new ConsoleApp(args[0]);
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
                string version = versionInfo.ProductVersion;
                Console.Title = "Aurora v" + version + " - " + Game.Instance.Name;

                Console.WriteLine("<" + DateTime.Now + "> [Aurora] Running in console-only mode.");
                consoleApp.Run();

                Console.WriteLine("<" + DateTime.Now + "> [Aurora] Good-bye!");
                FreeConsole();
            }
            else
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
