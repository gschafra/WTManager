﻿using System.Diagnostics;
using System.IO;

namespace WTManager.TrayMenu.MenuHandlers.Service
{
    public class ServiceDirectoryMenuItem : ServiceMenuItem
    {
        public ServiceDirectoryMenuItem(IWtTrayMenuController controller, Config.Service service) 
            : base(controller, service) { }

        protected override string DisplayText => "Open data directory…";

        protected override string ImageKey => "folder";

        protected override bool IsVisible => Directory.Exists(this.Service.DataDirectory);

        protected override void Action()
        {
            Process.Start($"http://{this.Service.BrowserUrl}");
        }
    }
}