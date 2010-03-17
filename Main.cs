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
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Hide();
            String[] args = Environment.GetCommandLineArgs();
            //MessageBox.Show(String.Join(", ", args));
            if (args.Length < 2)
            {
                MessageBox.Show("No application to start. Exiting.");
                Application.Exit();
                return;
            }

            try
            {
                p = new Process();
                p.StartInfo = new ProcessStartInfo(args[1]);
                if (args.Length > 2)
                {
                    p.StartInfo.Arguments = String.Join(" ", ((List<String>)args.ToList<String>().GetRange(2, args.Length - 2)).ToArray());
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
                //MessageBox.Show(hwnd.ToString());
                //MessageBox.Show(p.MainModule.FileName.ToString());
                p.Exited += new EventHandler(app_exited);
                notifyIcon.Text = p.MainWindowTitle;
                //notifyIcon.Icon = Icon.FromHandle(GetClassLongPtr(hwnd, 0));//largeIico;

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
            //MessageBox.Show(((Process)sender).ProcessName + " process has exited!");
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
            SetForegroundWindow(hwnd);
            //if (v)
            //    Thread.Sleep(100);
            //    SetFocus(hwnd);
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            togglevisibility();
        }
    }
}
