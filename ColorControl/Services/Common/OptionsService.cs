﻿using ColorControl.Shared.Common;
using ColorControl.Shared.Contracts;
using ColorControl.Shared.EventDispatcher;
using ColorControl.Shared.Services;
using ColorControl.UI;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ColorControl.Services.Common;

public class OptionsService
{
    private readonly GlobalContext _globalContext;
    private readonly ElevationService _elevationService;
    private readonly WinApiService _winApiService;
    private readonly ServiceManager _serviceManager;
    private readonly KeyboardShortcutDispatcher _keyboardShortcutDispatcher;
    private readonly WinApiAdminService _winApiAdminService;
    private readonly UpdateManager _updateManager;

    public OptionsService(GlobalContext globalContext, ElevationService elevationService, WinApiService winApiService, ServiceManager serviceManager, KeyboardShortcutDispatcher keyboardShortcutDispatcher, WinApiAdminService winApiAdminService, UpdateManager updateManager)
    {
        _globalContext = globalContext;
        _elevationService = elevationService;
        _winApiService = winApiService;
        _serviceManager = serviceManager;
        _keyboardShortcutDispatcher = keyboardShortcutDispatcher;
        _winApiAdminService = winApiAdminService;
        _updateManager = updateManager;
    }

    public Config GetConfig()
    {
        _globalContext.Config.AutoStart = _winApiService.TaskExists(Program.TS_TASKNAME);
        _globalContext.Config.CurrentUiPort = Blazor.GetCurrentPort();

        return _globalContext.Config;
    }

    public bool SetConfig(Config config)
    {
        if (_globalContext.Config.MinimizeToTray != config.MinimizeToTray)
        {
            SetMinimizeToTray(config.MinimizeToTray);
        }

        _globalContext.Config.Update(config);
        return true;
    }

    public bool SaveSettings()
    {
        _serviceManager.Save();
        return true;
    }

    public List<ModuleDto> GetModules()
    {
        return _globalContext.Config.Modules.Select(m => new ModuleDto { DisplayName = m.DisplayName, IsActive = m.IsActive, Info = _serviceManager.GetServiceInfo(m.DisplayName) }).ToList();
    }

    public async Task<InfoDto> GetInfo()
    {
        var updateInfo = await _updateManager.CheckForUpdates();

        return new InfoDto
        {
            ApplicationTitle = _globalContext.ApplicationTitle,
            ApplicationTitleAdmin = _globalContext.ApplicationTitleAdmin,
            DataPath = _globalContext.DataPath,
            StartTime = _globalContext.StartTime,
            UpdateInfoDto = updateInfo,
            LegalCopyright = _globalContext.LegalCopyright,
        };
    }

    public Task<bool> InstallUpdate()
    {
        return _updateManager.InstallUpdate();
    }

    public Task<bool> RestartAfterUpdate()
    {
        return _updateManager.RestartAfterUpdate();
    }

    public bool SetStartAfterLogin(bool value)
    {
        _elevationService.RegisterScheduledTask(value);

        return true;
    }

    public bool SetMinimizeOnClose(bool value)
    {
        _globalContext.Config.MinimizeOnClose = value;

        return true;
    }

    public bool SetStartMinimized(bool value)
    {
        _globalContext.Config.StartMinimized = value;

        return true;
    }

    public bool SetMinimizeToTray(bool value)
    {
        _globalContext.Config.MinimizeToTray = value;
        Program.GetNotifyIcon().Visible = _globalContext.Config.MinimizeToTray || _globalContext.Config.UiType != UiType.WinForms;

        return true;
    }

    public bool SetCheckForUpdates(bool value)
    {
        _globalContext.Config.CheckForUpdates = value;

        return true;
    }

    public bool SetAutoInstallUpdates(bool value)
    {
        _globalContext.Config.AutoInstallUpdates = value;

        return true;
    }

    public bool SetDarkMode(bool value)
    {
        _globalContext.Config.UseDarkMode = value;
        Program.SetTheme(value);

        return true;
    }

    public bool SetElevationMethod(ElevationMethod elevationMethod)
    {
        _elevationService.SetElevationMethod(elevationMethod);

        return true;
    }

    public bool SetUiType(UiType uiType)
    {
        _globalContext.Config.UiType = uiType;

        var _ = Program.StartOrStopUiServer();

        return true;
    }

    public bool SetUiPort(int uiPort)
    {
        if (uiPort is < 0 or > 65535)
        {
            return false;
        }

        if (_globalContext.Config.UiPort == uiPort)
        {
            return true;
        }

        _globalContext.Config.UiPort = uiPort;

        var _ = Program.StartOrStopUiServer();

        return true;
    }

    public bool SetUiAllowRemoteConnections(bool value)
    {
        _globalContext.Config.UiAllowRemoteConnections = value;

        var _ = Program.StartOrStopUiServer();

        return true;
    }

    public bool SetProcessMonitorPollingInterval(int pollingInterval)
    {
        _globalContext.Config.ProcessMonitorPollingInterval = pollingInterval;

        return true;
    }

    public bool SetUseRawInput(bool value)
    {
        _globalContext.Config.UseRawInput = value;

        _keyboardShortcutDispatcher.SetUseRawInput(value);

        return true;
    }

    public bool SetMinTmlAndMaxTml(bool value)
    {
        _globalContext.Config.SetMinTmlAndMaxTml = value;

        return true;
    }

    public bool SetDisableErrorPopupWhenApplyingPreset(bool value)
    {
        _globalContext.Config.DisableErrorPopupWhenApplyingPreset = value;

        return true;
    }

    public bool SetFixChromeFonts(bool value)
    {
        _globalContext.Config.FixChromeFonts = value;

        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (value != _winApiService.IsChromeFixInstalled())
        {
            return _winApiAdminService.InstallChromeFix(value, folder);
        }

        return true;
    }

    public bool SetScreenSaverShortcut(string value)
    {
        _globalContext.Config.ScreenSaverShortcut = value;

        return _keyboardShortcutDispatcher.RegisterShortcut(MainWorker.SHORTCUTID_SCREENSAVER, value);
    }

    public bool SetLogLevel(string logLevelName)
    {
        var logLevel = LogLevel.FromString(logLevelName);
        _globalContext.SetLogLevel(logLevel);

        return true;
    }

    public bool UpdateModule(Module updateModule)
    {
        var module = _globalContext.Config.Modules.FirstOrDefault(m => m.DisplayName == updateModule.DisplayName);

        if (module == null)
        {
            return false;
        }

        module.IsActive = updateModule.IsActive;

        var result = _serviceManager.ActivateModule(module);

        if (module.IsActive && !result)
        {
            module.IsActive = false;
        }

        return result;
    }
}

