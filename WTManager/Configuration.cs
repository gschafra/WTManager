﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using WTManager.Helpers;

namespace WTManager
{
    public class Configuration
    {
        private static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static readonly string ConfigPath = Path.Combine(AppData, "WTManager", "config.yml");

        private static readonly Lazy<Configuration> _configuration
            = new Lazy<Configuration>(() => {
                if (!File.Exists(ConfigPath)) {
                    SerializationHelpers.SerializeFile(ConfigPath, GetDefaults());
                }
                return SerializationHelpers.DeserializeFile<Configuration>(ConfigPath);
            });

        public Preferences Preferences { get; set; }
        public IEnumerable<Service> Services { get; set; }

        private static Configuration GetDefaults() {
            var conf = new Configuration {
                Preferences = new Preferences {
                    ShowBaloon = true,
                    BaloonTipTime = 3000
                },
                Services = new List<Service>()
            };
            return conf;
        }

        public static Configuration Config => _configuration.Value;
    }

    public class Preferences
    {
        public string EditorPath { get; set; }
        public bool ShowBaloon { get; set; }
        public int BaloonTipTime { get; set; }
    }

    public class ServiceCommand
    {
        /// <summary>
        /// Command name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Command argumetns
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Path to command
        /// </summary>
        public string Command { get; set; }
    }

    public class Service
    {
        public string ServiceName { get; set; }

        private string _displayName;
        public string DisplayName {
            get { return _displayName ?? ServiceName; }
            set { _displayName = value; }
        }

        private string _basePath;
        public string BasePath {
            get { return _basePath ?? string.Empty; }
            set { _basePath = value; }
        }

        private IEnumerable<string> _configFiles;
        public IEnumerable<string> ConfigFiles {
            get { return _configFiles?.Select(f => Path.Combine(BasePath, f)); }
            set { _configFiles = value; }
        }

        private IEnumerable<string> _logFiles;
        public IEnumerable<string> LogFiles {
            get { return _logFiles?.Select(f => Path.Combine(BasePath, f)); }
            set { _logFiles = value; }
        }

        private string _dataDirectory;
        public string DataDirectory {
            get { return Path.Combine(BasePath, _dataDirectory ?? String.Empty); }
            set { _dataDirectory = value; }
        }

        public IEnumerable<ServiceCommand> Commands { get; set; }
        public string BrowserUrl { get; set; }
        public string Group { get; set; }

        private ServiceController _controller;
        public ServiceController Controller {
            get
            {
                if (_controller == null) {
                    _controller = new ServiceController(ServiceName);
                }
                return _controller;
            }
        }
    }
}
