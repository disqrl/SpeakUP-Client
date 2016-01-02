﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.Runtime.InteropServices;
using SpeakUp.Controls;

namespace SpeakUp
{
    public partial class Form1 : Form
    {
        private ChromiumWebBrowser browser;
        private string appCss =
            "<style>" +
            "* {box-sizing: border-box}" +
            "body{margin:0; padding:0; font-family: Tahoma, Arial, Ubuntu, sans;background:#123 linear-gradient(#123, #135);}" +
            "h1{margin: 0 0 10px 0;}" +
            ".tryAgain{background:#157; color:#fff; padding:5px; border:1px solid #000; text-decoration:none; display: block; text-align: center;}" +
            ".tryAgain:hover{background:#27b;}" +
            ".block{max-width:800px; width:90%; border:1px solid #777; position: fixed;  top: 50%; left: 50%;" +
            "transform: translate(-50%,-50%); border-radius:5px; padding:20px; background: #eee; overflow: hidden;}" +
            "</style>";

        // P/Invoke constants
        private const int WM_SYSCOMMAND = 0x112;
        private const int MF_STRING = 0x0;
        private const int MF_SEPARATOR = 0x800;

        // P/Invoke declarations
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InsertMenu(IntPtr hMenu, int uPosition, int uFlags, int uIDNewItem, string lpNewItem);


        // ID for the system menu
        private int SYSMENU_DEV_TOOLS = 0x1;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            IntPtr hSysMenu = GetSystemMenu(this.Handle, false);
            AppendMenu(hSysMenu, MF_SEPARATOR, 0, string.Empty);
            AppendMenu(hSysMenu, MF_STRING, SYSMENU_DEV_TOOLS, "&Developer Tools");
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if ((m.Msg == WM_SYSCOMMAND) && ((int)m.WParam == SYSMENU_DEV_TOOLS))
            {
                browser.ShowDevTools();
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string url = Properties.Settings.Default.baseUrl;
            string instancePath = Application.StartupPath;
            var settings = new CefSettings();
            settings.CefCommandLineArgs.Add("enable-media-stream", "1");
            settings.CachePath = instancePath + @"\cache\";

            this.Left = Properties.Settings.Default.formX;
            this.Top = Properties.Settings.Default.formY;
            this.Width = Properties.Settings.Default.formW;
            this.Height = Properties.Settings.Default.formH;

            centerElements();

            Cef.Initialize(settings);

            browser = new ChromiumWebBrowser(url)
            {
                Dock = DockStyle.Fill
            };

            browser.TitleChanged += OnBrowserTitleChanged;
            browser.LoadError += OnBrowserLoadError;

            this.Controls.Add(browser);
        }

        private void OnBrowserTitleChanged(object sender, TitleChangedEventArgs args)
        {
            this.InvokeOnUiThreadIfRequired(() => Text = args.Title);
        }

        private string generateHtmlPage(string title, string text)
        {
            return "<html>" +
              "<head>" +
              "<title>" + title + "</title>" +
              appCss +
              "</head>" +
              "<body>" +
              "<div class='block'>" + text + "</div>" +
              "</body>" +
              "</html>";
        }

        private void OnBrowserLoadError(object sender, LoadErrorEventArgs args)
        {
            // //if (args.FailedUrl == browser.Address)
            // //{
            //     string dataurl = generateHtmlPage(
            //         "Error",
            //         "<h1>SpeakUP</h1>" +
            //         "<p><b>Connection error:</b> " + args.ErrorCode.ToString() + "</p>" +
            //         "<p><a class='tryAgain' href='" + args.FailedUrl + "'>Try Again</a></p>");
            //     browser.LoadHtml(dataurl, args.FailedUrl);
            // //}

            this.InvokeOnUiThreadIfRequired(() =>
            {
                errLabelStatus.Text = "Connection error: " + args.ErrorCode.ToString();
                errPanel.Visible = true;
                browser.Visible = false;
            });
        }

        private void centerElements()
        {
            errPanel.Left = this.Width / 2 - errPanel.Width / 2;
            errPanel.Top = this.Height / 2 - errPanel.Height / 2;
        }

        private void saveSizePos(bool saveSettings = true)
        {
            bool formMaximized = (this.WindowState == FormWindowState.Maximized);
            if (!formMaximized)
            {
                Properties.Settings.Default.formMaximized = formMaximized;
                Properties.Settings.Default.formX = this.Left;
                Properties.Settings.Default.formY = this.Top;
                Properties.Settings.Default.formW = this.Width;
                Properties.Settings.Default.formH = this.Height;
            }
            if (saveSettings)
            {
                Properties.Settings.Default.Save();
            }
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            saveSizePos();
        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            saveSizePos();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            saveSizePos(false);
            // save other settings...
            Properties.Settings.Default.Save();
        }

        private void errBtnReload_Click(object sender, EventArgs e)
        {
            errPanel.Visible = false;
            browser.Visible = true;
            browser.Reload();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            centerElements();
        }
    }
}
