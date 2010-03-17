using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;


namespace AppTrayer
{
    public partial class Main : Form
    {
        public const int GCL_HICONSM = -34;
        public const int GCL_HICON = -14;

        public const int ICON_SMALL = 0;
        public const int ICON_BIG = 1;
        public const int ICON_SMALL2 = 2;

        public const int WM_GETICON = 0x7F;

        public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size > 4)
                return GetClassLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetClassLongPtr32(hWnd, nIndex));
        }

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        public static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        public static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern uint ExtractIconEx(string szFileName, int nIconIndex,
           IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        Process p;
        IntPtr hwnd;
        Thread t;
        bool visible = true;

        public Main()
        {
            InitializeComponent();

            String[] args = Environment.GetCommandLineArgs();
            Dictionary<string, string> switches = new Dictionary<string, string>();
            List<string> targetargs = new List<string>();
            bool switchend = false;
            var subset = args.Skip(1).Take(args.Length - 1);
            foreach (string arg in subset)
            {
                if (arg.StartsWith("--") && !switchend)
                {
                    string[] s = arg.Split('=');
                    switches.Add(s[0].Substring(2, s[0].Length - 2), (s.Length > 1) ? s[1] : null);
                }
                else
                {
                    switchend = true;
                    targetargs.Add(arg);
                }
            }

            if (targetargs.Count < 1)
            {
                MessageBox.Show("No application to start. Exiting.");
                Application.Exit();
                return;
            }

            try
            {
                p = new Process();
                p.StartInfo = new ProcessStartInfo(targetargs[0]);
                if (args.Length > 2)
                {
                    p.StartInfo.Arguments = String.Join(" ", targetargs.GetRange(1, targetargs.Count - 1).ToArray());
                }
                if (switches.ContainsKey("minimize") || switches.ContainsKey("minimized"))
                {
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                }
                p.EnableRaisingEvents = true;
                p.Start();
                hwnd = p.MainWindowHandle;
                while (hwnd == IntPtr.Zero)
                {
                    System.Threading.Thread.Sleep(100);
                    p.Refresh();
                    hwnd = p.MainWindowHandle;
                }

                p.Exited += new EventHandler(app_exited);
                if (switches.ContainsKey("minimize") || switches.ContainsKey("minimized"))
                    setVisible(false);
                notifyIcon.Text = p.MainWindowTitle;

                bool icon_is_set = false;
                if (switches.ContainsKey("icon"))
                {
                    try
                    {
                        notifyIcon.Icon = new System.Drawing.Icon(switches["icon"]);
                        icon_is_set = true;
                    }
                    //invalid icon path
                    catch (ArgumentException) { }
                    catch (System.IO.FileNotFoundException) { }

                }

                if (!icon_is_set)
                {
                    IntPtr hIcon = (IntPtr)SendMessage(hwnd, WM_GETICON, ICON_SMALL2, 0);
                    if (hIcon == IntPtr.Zero)
                        hIcon = (IntPtr)SendMessage(hwnd, WM_GETICON, ICON_SMALL, 0);
                    if (hIcon == IntPtr.Zero)
                        hIcon = (IntPtr)SendMessage(hwnd, WM_GETICON, ICON_BIG, 0);
                    if (hIcon == IntPtr.Zero)
                        hIcon = (IntPtr)GetClassLongPtr(hwnd, GCL_HICONSM);
                    if (hIcon == IntPtr.Zero)
                        hIcon = GetClassLongPtr(hwnd, GCL_HICON);
                    if (hIcon == IntPtr.Zero)
                    {
                        IntPtr[] largeIcon = new IntPtr[1];
                        IntPtr[] smallIcon = new IntPtr[1];
                        ExtractIconEx(p.MainModule.FileName.ToString(), 0, largeIcon, smallIcon, 1);
                        hIcon = smallIcon[0];
                        if (hIcon == IntPtr.Zero)
                            hIcon = largeIcon[0];
                    }
                    if (hIcon != IntPtr.Zero)
                        notifyIcon.Icon = Icon.FromHandle(hIcon);
                }
                t = new Thread(check_winstate);
                t.Start();

            }
            catch (System.ComponentModel.Win32Exception er)
            {
                MessageBox.Show(er.Message);
                Application.Exit();
                return;
            }

        
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Hide();
        }

        void check_winstate()
        {
            while (true)
            {
                if (IsIconic(hwnd))
                {
                    setVisible(false);
                }
                System.Threading.Thread.Sleep(500);
            }
        }

        void app_exited(object sender, EventArgs e)
        {
            t.Abort();
            Application.Exit();
        }

        void togglevisibility()
        {
            if (visible)
            {
                setVisible(false);
            }
            else
            {
                setVisible(true);
            }
        }

        void setVisible(bool v)
        {
            ShowWindow(hwnd, v ? 1 : 0);
            visible = v;
            if (v)
                SetForegroundWindow(hwnd);
            this.minimizeToolStripMenuItem.Visible = v;
            this.restoreToolStripMenuItem.Visible = !v;

        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                togglevisibility();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            p.Kill();
        }

        private void minimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setVisible(false);
        }

        private void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setVisible(true);
        }
    }
}
