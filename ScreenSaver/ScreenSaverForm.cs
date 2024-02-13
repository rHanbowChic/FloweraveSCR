/*
 * ScreenSaverForm.cs
 * By Frank McCown
 * Summer 2010
 * 
 * Feel free to modify this code.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;
using Microsoft.Web.WebView2.Core;
using System.IO.Compression;


namespace FloweraveSCR
{
    public partial class ScreenSaverForm : Form
    {
        #region Win32 API functions

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

        #endregion


        private Point mouseLocation;
        private bool previewMode = false;
        private Random rand = new Random();
        private string flveTempPath;

        public void deleteDir(string PATH)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(PATH);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public ScreenSaverForm()
        {
            InitializeComponent();
        }

        public ScreenSaverForm(Rectangle Bounds)
        {
            InitializeComponent();
            this.Bounds = Bounds;
        }

        public ScreenSaverForm(IntPtr PreviewWndHandle)
        {
            InitializeComponent();

            // Set the preview window as the parent of this window
            SetParent(this.Handle, PreviewWndHandle);

            // Make this a child window so it will close when the parent dialog closes
            SetWindowLong(this.Handle, -16, new IntPtr(GetWindowLong(this.Handle, -16) | 0x40000000));

            // Place our window inside the parent
            Rectangle ParentRect;
            GetClientRect(PreviewWndHandle, out ParentRect);
            Size = ParentRect.Size;
            Location = new Point(0, 0);

            

            previewMode = true;
        }

        private async void ScreenSaverForm_Load(object sender, EventArgs e)
        {

            string tempPath = Path.GetTempPath();
            flveTempPath = Path.Combine(tempPath, "FloweraveResources");
            string wv2TempPath = Path.Combine(tempPath, "FloweraveWebView2");
            if (Directory.Exists(flveTempPath))
                deleteDir(flveTempPath);
            else if (File.Exists(flveTempPath))
                File.Delete(flveTempPath);


            CoreWebView2Environment corewv2Env = await CoreWebView2Environment.CreateAsync(userDataFolder: wv2TempPath);
            await this.webView.EnsureCoreWebView2Async(corewv2Env);

            MemoryStream flveStream = new MemoryStream(Properties.Resources.flowerave_resources);
            ZipArchive flveArchive = new ZipArchive(flveStream);

            flveArchive.ExtractToDirectory(flveTempPath);

            string timeInterval = "5000";
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\FloweraveSCR");
            if (key != null)
                timeInterval = ((int)key.GetValue("Interval")).ToString();
            string flveIndexPath = Path.Combine(flveTempPath, "index.html");
            string flveIndex = File.ReadAllText(flveIndexPath);
            flveIndex = flveIndex.Replace("\"1234HASH4321\"", timeInterval);
            File.WriteAllText(flveIndexPath, flveIndex);

            this.webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            this.webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            this.webView.Source = new System.Uri(flveIndexPath, System.UriKind.Absolute);

            Cursor.Hide();            
            TopMost = true;

            
        }

        

        

        private void ScreenSaverForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!previewMode)
            {
                if (!mouseLocation.IsEmpty)
                {
                    // Terminate if mouse is moved a significant distance
                    if (Math.Abs(mouseLocation.X - e.X) > 0 ||
                        Math.Abs(mouseLocation.Y - e.Y) > 0)
                        Application.Exit();
                }

                // Update current mouse location
                mouseLocation = e.Location;
            }
        }

        private void ScreenSaverForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!previewMode)
                Application.Exit();
        }

        private void ScreenSaverForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (!previewMode)
                Application.Exit();
        }

        private void ScreenSaverForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Directory.Exists(flveTempPath))
                deleteDir(flveTempPath);
        }
    }
}
