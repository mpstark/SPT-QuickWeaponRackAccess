using Aki.Reflection.Utils;
using BepInEx;
using BepInEx.Logging;
using DrakiaXYZ.VersionChecker;
using EFT.InventoryLogic;
using EFT.UI;
using QuickWeaponRackAccess.Patches;
using QuickWeaponRackAccess.Utils;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace QuickWeaponRackAccess
{
    [BepInPlugin("com.mpstark.QuickWeaponRackAccess", "QuickWeaponRackAccess", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 29197;
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;
        public static Inventory PlayerInventory => ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Inventory;
        public static string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string IconPath = Path.Combine(PluginFolder, "icon.png");

        public QuickWeaponRackComponent QuickWeaponRackComponent => _quickWeaponRackComponent;

        private GameObject _quickWeaponRackContainerGO;
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

        public void TryAttachToInventoryScreen(InventoryScreen inventoryScreen)
        {
            // only attach if not already attached or hideout isn't upgraded
            if (_quickWeaponRackContainerGO != null || !PlayerInventory.HideoutAreaStashes.ContainsKey(EFT.EAreaType.WeaponStand))
            {
                return;
            }

            // this is expensive, and it's only called once
            var sortButtonTemplate = GameObject.Find("Common UI/Common UI/InventoryScreen/Items Panel/Stash Panel/Simple Panel/Sorting Panel/SortTableButton");

            // create button game object from template
            var buttonGO = Instantiate(sortButtonTemplate, sortButtonTemplate.transform.parent);
            buttonGO.name = "QuickWeaponRackAccessTab";
            buttonGO.transform.SetAsFirstSibling();

            // change icon of button
            var image = buttonGO.transform.Find("Image").GetComponent<Image>();
            var texture = TextureUtils.LoadTexture2DFromPath(IconPath);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), image.sprite.pivot);

            // setup window
            _quickWeaponRackContainerGO = new GameObject("QuickWeaponRackAccessContainer");
            _quickWeaponRackContainerGO.transform.SetParent(inventoryScreen.gameObject.transform);
            _quickWeaponRackContainerGO.transform.localScale = Vector3.one;
            _quickWeaponRackContainerGO.SetActive(false);
            _quickWeaponRackComponent = _quickWeaponRackContainerGO.AddComponent<QuickWeaponRackComponent>();

            // setup tab
            var tab = buttonGO.GetComponent<Tab>();
            tab.UpdateVisual(false);
            tab.OnSelectionChanged += (_, selected) => {
                if (selected)
                {
                    tab.Select();
                    _quickWeaponRackContainerGO.SetActive(true);
                    _quickWeaponRackComponent.Show(() => tab.Deselect());
                }
            };
        }
    }
}
