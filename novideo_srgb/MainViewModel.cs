﻿using Microsoft.Win32;
using NvAPIWrapper.Display;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;

namespace novideo_srgb
{
    public class MainViewModel
    {
        public static string ConfigPath { get; set; } = "";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public ObservableCollection<MonitorData> Monitors { get; }

        private string _configPath;

        public MainViewModel()
        {
            Monitors = new ObservableCollection<MonitorData>();
            _configPath = Path.Combine(ConfigPath, "NovideoConfig.xml");

            UpdateMonitors();
        }

        public void UpdateMonitors(bool? forceClamp = null)
        {
            Monitors.Clear();
            List<XElement>? config = null;
            if (File.Exists(_configPath))
            {
                config = XElement.Load(_configPath).Descendants("monitor").ToList();
            }

            var hdrPaths = Logger.Swallow(DisplayConfigManager.GetHdrDisplayPaths, []);
            var displays = WindowsDisplayAPI.Display.GetDisplays();

            var number = 1;
            foreach (var display in GetDisplays())
            {
                var path = displays.FirstOrDefault(x => x.DisplayName == display.Name)?.DevicePath;
                if (path == null)
                {
                    continue;
                }

                try
                {
                    var hdrActive = hdrPaths.Contains(path);

                    var settings = config?.FirstOrDefault(x => (string?)x.Attribute("path") == path);
                    MonitorData monitor;
                    if (settings != null)
                    {
                        monitor = new MonitorData(this, number++, display, path, hdrActive,
                            (bool?)settings.Attribute("clamp_sdr") ?? false,
                            (bool?)settings.Attribute("use_icc") ?? false,
                            (string?)settings.Attribute("icc_path") ?? "",
                            (bool?)settings.Attribute("calibrate_gamma") ?? false,
                            (int?)settings.Attribute("selected_gamma") ?? 0,
                            (double?)settings.Attribute("custom_gamma") ?? 2.2,
                            (double?)settings.Attribute("custom_percentage") ?? 100,
                            (int?)settings.Attribute("target") ?? 0,
                            (bool?)settings.Attribute("disable_optimization") ?? false);
                    }
                    else
                    {
                        monitor = new MonitorData(this, number++, display, path, hdrActive, false);
                    }

                    Monitors.Add(monitor);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Error while getting monitor data for {display.Name}: {ex.Message}");
                }
            }

            foreach (var monitor in Monitors)
            {
                monitor.ReapplyClamp(forceClamp);
            }
        }

        private static Display[] GetDisplays()
        {
            try
            {
                return Display.GetDisplays();
            }
            catch (Exception)
            {
                return Array.Empty<Display>();
            }
        }

        public async void OnDisplaySettingsChanged(object? sender, EventArgs? e)
        {
            await Task.Delay(1000);
            UpdateMonitors();
        }

        public void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.Resume) return;
            OnDisplaySettingsChanged(null, null);
        }

        public void SaveConfig()
        {
            try
            {
                var xElem = new XElement("monitors",
                    Monitors.Select(x =>
                        new XElement("monitor", new XAttribute("path", x.Path),
                            new XAttribute("clamp_sdr", x.ClampSdr),
                            new XAttribute("use_icc", x.UseIcc),
                            new XAttribute("icc_path", x.ProfilePath),
                            new XAttribute("calibrate_gamma", x.CalibrateGamma),
                            new XAttribute("selected_gamma", x.SelectedGamma),
                            new XAttribute("custom_gamma", x.CustomGamma),
                            new XAttribute("custom_percentage", x.CustomPercentage),
                            new XAttribute("target", x.Target),
                            new XAttribute("disable_optimization", x.DisableOptimization))));
                xElem.Save(_configPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\nTry extracting the program elsewhere.");
                Environment.Exit(1);
            }
        }
    }
}