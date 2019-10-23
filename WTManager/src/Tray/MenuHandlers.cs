﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Forms;
using WtManager.Config;
using WtManager.Controls.WtStyle;
using WtManager.Forms;
using WtManager.Helpers;
using WtManager.Resources;

namespace WtManager.Tray
{
    #region Base abstract specialized menu items

    /// <summary>
    /// Base service related menu item, contains reference to linked service
    /// </summary>
    public abstract class ServiceMenuItem : WtMenuItem
    {
        protected Service Service { get; private set; }

        protected ServiceMenuItem(ITrayController controller, Service service)
            : base(controller)
        {
            this.Service = service;
        }
    }

    public abstract class FileOperationMenuItem : WtMenuItem
    {
        protected string FileName { get; private set; }

        protected FileOperationMenuItem(ITrayController controller, string fileName)
            : base(controller)
        {
            this.FileName = fileName;
        }
    }

    public abstract class ServiceGroupOperationMenuItem : WtMenuItem
    {
        protected string GroupName { get; private set; }

        protected ServiceGroupOperationMenuItem(ITrayController controller, string groupName)
            : base(controller)
        {
            this.GroupName = groupName;
        }
    }

    #endregion Base abstract specialized menu items

    #region Service related menu items

    /// <summary>
    /// Service top menu item, contain all other sub items
    /// </summary>
    public class ServiceTopMenuItem : ServiceMenuItem
    {
        public ServiceTopMenuItem(ITrayController controller, Service service)
            : base(controller, service) { }

        protected override string DisplayText => this.Service.DisplayName;

        protected override bool IsEnabled => !this.Service.Controller.IsInPendingState();

        protected override string ImageKey
        {
            get
            {
                if (this.Service.Controller == null)
                {
                    return "service-status-na";
                }

                if (this.Service.Controller.Status == ServiceControllerStatus.Running)
                {
                    return "service-status-started";
                }

                if (this.Service.Controller.Status == ServiceControllerStatus.Stopped)
                {
                    return "service-status-stopped";
                }

                if (this.Service.Controller.IsInPendingState())
                {
                    return "service-status-pending";
                }

                return base.ImageKey;
            }
        }
    }

    /// <summary>
    /// "Edit configuration" service menu item
    /// </summary>
    public class ServiceConfigMenuItem : FileOperationMenuItem
    {
        public ServiceConfigMenuItem(ITrayController controller, string fileName)
            : base(controller, fileName) { }

        protected override string DisplayText
            => $"Edit {Path.GetFileName(this.FileName)}";

        protected override string ImageKey => "service-edit-config";

        protected override void Action()
        {
            bool isValidEditor = File.Exists(ConfigManager.Instance.Config.ConfigEditorPath);

            // if config editor wasn't set we will just use default notepad application
            string editorPath = !isValidEditor
                ? "notepad.exe"
                : ConfigManager.Instance.Config.ConfigEditorPath;

            Process.Start(editorPath, this.FileName);
        }
    }

    /// <summary>
    /// "Open data directory" service menu item
    /// </summary>
    public class ServiceDirectoryMenuItem : ServiceMenuItem
    {
        public ServiceDirectoryMenuItem(ITrayController controller, Service service)
            : base(controller, service) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceOpenDataDirectory");

        protected override string ImageKey => "service-open-data-directory";

        protected override bool IsVisible => Directory.Exists(this.Service.DataDirectory);

        protected override void Action()
        {
            Process.Start(this.Service.DataDirectory);
        }
    }

    /// <summary>
    /// "Open in browser" service memu item
    /// </summary>
    public class ServiceBrowserMenuItem : ServiceMenuItem
    {
        public ServiceBrowserMenuItem(ITrayController controller, Service service)
            : base(controller, service) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceOpenInBrower");

        protected override string ImageKey => "service-open-browser";

        protected override bool IsVisible => !String.IsNullOrEmpty(this.Service.BrowserUrl);

        protected override void Action()
        {
            Process.Start($"http://{this.Service.BrowserUrl}");
        }
    }

    /// <summary>
    /// Service signle log entity menu item
    /// </summary>
    public class ServiceLogMenuItem : FileOperationMenuItem
    {
        public ServiceLogMenuItem(ITrayController controller, string fileName)
            : base(controller, fileName) { }

        protected override string DisplayText
            => $"Show {Path.GetFileName(this.FileName)}";

        protected override string ImageKey => "service-show-log";

        protected override void Action()
        {
            string viewer = ConfigManager.Instance.Config.LogViewerPath;

            if (string.IsNullOrEmpty(viewer) || viewer == "internal")
            {
                new LogFileViewerForm(this.FileName).Show();
                return;
            }

            if (File.Exists(viewer))
            {
                Process.Start(viewer, this.FileName);
            }
            else
            {
                MessageBox.Show($"Can't use selected log viewer ({viewer}), check your configuration");
            }
        }
    }

    public class ServiceEditMenuItem : ServiceMenuItem
    {
        public ServiceEditMenuItem(ITrayController controller, Service service)
            : base(controller, service) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceEditConfiguration");

        protected override string ImageKey => "service-config";

        protected override void Action()
        {
            //if (AddEditServiceForm.EditItem(this.Service) != null)
            //    ConfigManager.Instance.SaveConfig();
        }
    }

    public class ServiceRestartMenuItem : ServiceMenuItem
    {
        public ServiceRestartMenuItem(ITrayController controller, Service service)
            : base(controller, service) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceRestart");

        protected override string ImageKey => "service-restart";

        protected override bool IsVisible
            => this.Service.Controller?.Status == ServiceControllerStatus.Running;

        protected override async void Action()
        {
            await Task.Factory.StartNew(this.Service.Controller.RestartService);
            this.Controller.ShowBaloon("Started", $"Service {this.Service.DisplayName} was restarted", ToolTipIcon.Info);
        }
    }

    public class ServiceStartMenuItem : ServiceMenuItem
    {
        public ServiceStartMenuItem(ITrayController controller, Service service)
            : base(controller, service) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceStart");

        protected override string ImageKey => "service-start";

        protected override bool IsVisible
            => this.Service.Controller?.Status == ServiceControllerStatus.Stopped;

        protected override async void Action()
        {
            await Task.Factory.StartNew(this.Service.Controller.StartService);
            this.Controller.ShowBaloon("Started", $"Service {this.Service.DisplayName} was started", ToolTipIcon.Info);
        }
    }

    public class ServiceStopMenuItem : ServiceMenuItem
    {
        public ServiceStopMenuItem(ITrayController controller, Service service)
            : base(controller, service) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceStop");

        protected override string ImageKey => "service-stop";

        protected override bool IsVisible
            => this.Service.Controller != null && this.Service.Controller.Status == ServiceControllerStatus.Running;

        protected override async void Action()
        {
            await Task.Factory.StartNew(this.Service.Controller.StopService);
            this.Controller.ShowBaloon("Started", $"Service {this.Service.DisplayName} was stopped", ToolTipIcon.Info);
        }
    }

    #endregion Service related menu items

    #region Service group related menu items

    public class ServiceGroupMenuItem : WtMenuItem
    {
        private string GroupName { get; set; }

        protected override string ImageKey => "service-group";

        public ServiceGroupMenuItem(ITrayController controller, string groupName)
            : base(controller)
        {
            this.GroupName = groupName;
        }

        private string DisplayGroupName
            => String.IsNullOrEmpty(this.GroupName) ? "<" + LocalizationManager.Get("TrayMenu.ServiceGroup.Ungrouped") + ">" : this.GroupName;

        protected override string DisplayText => $"{this.DisplayGroupName} ({this.GetStartedServicesInfo()} started)";

        private string GetStartedServicesInfo()
        {
            var services = ServiceHelpers.GetServicesByGroupName(this.GroupName).ToList();
            int startedCount = services.Count(s => s.Controller.Status == ServiceControllerStatus.Running);
            return $"{startedCount} of {services.Count}";
        }
    }

    public class ServiceGroupStartMenuItem : ServiceGroupOperationMenuItem
    {
        public ServiceGroupStartMenuItem(ITrayController controller, string groupName)
            : base(controller, groupName) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceGroupStart");

        protected override string ImageKey => "service-start";

        protected override async void Action()
        {
            await Task.Factory.StartNew(() => ServiceHelpers.StartServiceGroup(this.GroupName));
            this.Controller.ShowBaloon("Stopped", $"All services in group {this.GroupName} was strated", ToolTipIcon.Info);
        }
    }

    public class ServiceGroupStopMenuItem : ServiceGroupOperationMenuItem
    {
        public ServiceGroupStopMenuItem(ITrayController controller, string groupName)
            : base(controller, groupName) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceGroupStop");

        protected override string ImageKey => "service-stop";

        protected override async void Action()
        {
            await Task.Factory.StartNew(() => ServiceHelpers.StopServiceGroup(this.GroupName));
            this.Controller.ShowBaloon("Stopped", $"All services in group {this.GroupName} was stopped", ToolTipIcon.Info);
        }
    }

    public class ServiceGroupRestartMenuItem : ServiceGroupOperationMenuItem
    {
        public ServiceGroupRestartMenuItem(ITrayController controller, string groupName)
            : base(controller, groupName) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceGroupRestart");

        protected override string ImageKey => "service-restart";

        protected override async void Action()
        {
            await Task.Factory.StartNew(() => ServiceHelpers.RestartServiceGroup(this.GroupName));
            this.Controller.ShowBaloon("Stopped", $"All services in group {this.GroupName} was restarted", ToolTipIcon.Info);
        }
    }

    #endregion Service group related menu items

    #region Root menu items

    public class ServiceTasksManagerMenuItem : WtMenuItem
    {
        public ServiceTasksManagerMenuItem(ITrayController controller)
            : base(controller) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ServiceScheduler");

        protected override string ImageKey => "services-scheduler";

        protected override void Action()
        {
            var dialog = new WtDialog();

            var parameters = new DialogItem(ConfigManager.Instance.Config, control =>
            {
                control.FillLastControl = true;
                control.FillLastGroup = true;
                control.LabelConfiguration.ShowLables = false;
            });
            parameters.AddGroup(Configuration.GROUP_TASKS);
            dialog.AddVisualSourceObject(parameters);

            dialog.ShowModal();
        }
    }

    public class SystemServicesManagerMenuItem : WtMenuItem
    {
        public SystemServicesManagerMenuItem(ITrayController controller)
            : base(controller) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.SystemServiceManager");

        protected override string ImageKey => "system-services-manager";

        protected override void Action()
        {
            Process.Start("services.msc");
        }
    }

    public class ApplicationConfigMenuItem : WtMenuItem
    {
        public ApplicationConfigMenuItem(ITrayController controller)
            : base(controller) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.ProgramConfiguration");

        protected override string ImageKey => "settings-manager";

        protected override void Action()
        {
            var dialog = new WtDialog();

            var basicPart = new DialogItem(ConfigManager.Instance.Config);
            basicPart.AddGroup(Configuration.GROUP_GENERAL);
            basicPart.AddGroup(Configuration.GROUP_SYSTEM);
            basicPart.AddGroup(Configuration.GROUP_UI);
            basicPart.Scale = 0.6f;
            dialog.AddVisualSourceObject(basicPart);

            var servicesPart = new DialogItem(ConfigManager.Instance.Config, control =>
            {
                control.FillLastControl = true;
                control.FillLastGroup = true;
                control.LabelConfiguration.ShowLables = false;
            });
            servicesPart.AddGroup(Configuration.GROUP_SERVICES);
            dialog.AddVisualSourceObject(servicesPart);

            dialog.ShowModal();
        }
    }

    public class ApplicationExitMenuItem : WtMenuItem
    {
        public ApplicationExitMenuItem(ITrayController controller)
            : base(controller) { }

        protected override string DisplayText => LocalizationManager.Get("TrayMenu.AppExit");

        protected override string ImageKey => "app-exit";

        protected override void Action()
        {
            Application.Exit();
        }
    }

    #endregion Root menu items

    #region Menu handlers

    public class TitleMenuItem : WtMenuItem
    {
        protected override Font ItemFont => ConfigManager.Instance.Config.MenuTitleFont;

        protected override string DisplayText { get; }

        protected override bool IsEnabled => false;

        public TitleMenuItem(ITrayController controller, string text)
            : base(controller)
        {
            this.DisplayText = text;
        }
    }

    public class SeparatorMenuItem : WtMenuItem
    {
        public SeparatorMenuItem(ITrayController controller)
            : base(controller) { }

        protected override ToolStripItem ToMenuItem()
        {
            return new ToolStripSeparator();
        }
    }

    #endregion Menu handlers
}