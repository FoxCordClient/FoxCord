using System;
using System.Windows.Forms;

namespace FoxCord
{
    public class SysTrayManager : IDisposable
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private readonly FoxCord mainForm;

        public SysTrayManager(FoxCord form)
        {
            mainForm = form;

            trayMenu = new ContextMenuStrip();

            var openItem = new ToolStripMenuItem("Open FoxCord", null, Open_Click);
            var exitItem = new ToolStripMenuItem("Exit FoxCord", null, Exit_Click);

            trayMenu.Items.Add(openItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon
            {
                Text = "FoxCord",
                Icon = mainForm.Icon,
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.MouseClick += TrayIcon_MouseClick;
        }

        private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                RestoreForm();
            }
        }

        private void Open_Click(object? sender, EventArgs e)
        {
            RestoreForm();
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            mainForm.ExitApplication();
        }

        private void RestoreForm()
        {
            mainForm.Show();

            if (mainForm.WindowState == FormWindowState.Minimized)
                mainForm.WindowState = FormWindowState.Normal;

            mainForm.BringToFront();
            mainForm.Activate();
        }

        public void Dispose()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
        }
    }
}