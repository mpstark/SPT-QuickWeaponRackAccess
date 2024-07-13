using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using DrakiaXYZ.VersionChecker;
using EFT.InventoryLogic;
using EFT.UI;
using QuickWeaponRackAccess.Patches;
using SPT.Reflection.Utils;

namespace QuickWeaponRackAccess
{
    // the version number here is generated on build and may have a warning if not yet built
    [BepInPlugin("com.mpstark.QuickWeaponRackAccess", "QuickWeaponRackAccess", BuildInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 30626;
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;
        public static Inventory PlayerInventory => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Inventory;
        public static string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public QuickWeaponRackComponent QuickWeaponRackComponent => _quickWeaponRackComponent;

        private QuickWeaponRackComponent _quickWeaponRackComponent;

        internal void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            Instance = this;
            DontDestroyOnLoad(this);

            // patches
            new InventoryScreenShowPatch().Enable();
            new QuickFindAppropriatePlacePatch().Enable();
        }

        /// <summary>
        /// Try attach the tab to the inventory screen, but only if hideout isn't upgraded
        /// </summary>
        public void TryAttachToInventoryScreen(InventoryScreen inventoryScreen)
        {
            // only attach if not already attached or hideout isn't upgraded
            if (_quickWeaponRackComponent != null || !PlayerInventory.HideoutAreaStashes.ContainsKey(EFT.EAreaType.WeaponStand))
            {
                return;
            }

            _quickWeaponRackComponent = QuickWeaponRackComponent.AttachToInventoryScreen(inventoryScreen);
        }
    }
}
