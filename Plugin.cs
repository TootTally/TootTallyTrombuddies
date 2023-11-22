using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;
using UnityEngine;

namespace TootTallyTrombuddies
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyAccounts", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "Trombuddies.cfg";
        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        //Change this name to whatever you want
        public string Name { get => "Trombuddies"; set => Name = value; }

        public static TootTallySettingPage settingPage;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            _harmony = new Harmony(Info.Metadata.GUID);

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "Trombuddies", true, "Friend list system for TootTallyCore");
            TootTallyModuleManager.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true) { SaveOnConfigSet = true };
            TogglePanel = config.Bind("Keybinds", "TogglePanel", KeyCode.F2, "Toggle the Trombuddies Panel.");
            ToggleFriendOnly = config.Bind("Keybinds", "ToggleFriendOnly", KeyCode.F3, "Toggle show friends only.");
            ToggleOnlineOnly = config.Bind("Keybinds", "ToggleOnlineOnly", KeyCode.F4, "Toggle show online users only.");

            settingPage = TootTallySettingsManager.AddNewPage("Trombuddies", "Trombuddies", 40f, new Color(0, 0, 0, 0));
            settingPage.AddLabel("TogglePanelLabel", "Toggle Panel Keybind", 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            settingPage.AddDropdown("Toggle Panel Keybind", TogglePanel);
            settingPage.AddLabel("ToggleFriendsLabel", "Toggle Friends Only Keybind", 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            settingPage.AddDropdown("Toggle Friends Only Keybind", ToggleFriendOnly);
            settingPage.AddLabel("ToggleOnlineLabel", "Toggle Online Only Keybind", 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            settingPage.AddDropdown("Toggle Online Only Keybind", ToggleOnlineOnly);

            _harmony.PatchAll(typeof(TrombuddiesGameObjectFactory));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            TrombuddiesGameObjectFactory.Dispose();
            _harmony.UnpatchSelf();
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public ConfigEntry<KeyCode> TogglePanel { get; set; }
        public ConfigEntry<KeyCode> ToggleFriendOnly { get; set; }
        public ConfigEntry<KeyCode> ToggleOnlineOnly { get; set; }
    }
}