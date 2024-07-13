using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT.InputSystem;
using EFT.UI;
using EFT.UI.Chat;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using QuickWeaponRackAccess.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace QuickWeaponRackAccess
{
    public class QuickWeaponRackComponent : UIInputNode
    {
        private static FieldInfo _inventoryScreenSimpleStashPanelField = AccessTools.Field(typeof(InventoryScreen), "_simpleStashPanel");
        private static FieldInfo _simpleStashPanelSortingTableTabField = AccessTools.Field(typeof(SimpleStashPanel), "_sortingTableTab");
        private static FieldInfo _itemUiContextInventoryControllerField = AccessTools.GetDeclaredFields(typeof(ItemUiContext)).Single(x => x.FieldType == typeof(InventoryControllerClass));
        private static FieldInfo _inputNodeAbstractChildrenField = AccessTools.Field(typeof(InputNodeAbstract), "_children");
        private static FieldInfo _chatScreenCloseButtonField = AccessTools.Field(typeof(ChatScreen), "_closeButton");

        private static string _iconPath = Path.Combine(Plugin.PluginFolder, "icon.png");

        public override RectTransform RectTransform => _windowTransform;

        public LootItemClass ActiveWeaponRackContainer => _activeContainer;

        private List<InputNode> ItemUiContextChildren => _inputNodeAbstractChildrenField.GetValue(ItemUiContext.Instance) as List<InputNode>;

        private LootItemClass _activeContainer;
        private ContainedGridsView _gridView;
        private RectTransform _windowTransform;
        private Vector2 _windowAnchorPosition = new Vector2(360, 315);
        private GameObject _closeButtonTemplate;
        private Action _onClosed;
        private EFT.EAreaType _lastArea = EFT.EAreaType.WeaponStand;

        public static QuickWeaponRackComponent AttachToInventoryScreen(InventoryScreen inventoryScreen)
        {
            // find sort button to use as a template
            var simpleStashPanel = _inventoryScreenSimpleStashPanelField.GetValue(inventoryScreen) as SimpleStashPanel;
            var sortButtonTab = _simpleStashPanelSortingTableTabField.GetValue(simpleStashPanel) as Tab;
            var sortButtonTemplate = sortButtonTab.gameObject;

            // create button game object from template
            var buttonGO = Instantiate(sortButtonTemplate, sortButtonTemplate.transform.parent);
            buttonGO.name = "QuickWeaponRackAccessTab";
            buttonGO.transform.SetAsFirstSibling();

            // change icon of button
            var image = buttonGO.transform.Find("Image").GetComponent<Image>();
            var texture = TextureUtils.LoadTexture2DFromPath(_iconPath);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), image.sprite.pivot);

            // setup container
            var containerGO = new GameObject("QuickWeaponRackAccessContainer");
            containerGO.transform.SetParent(inventoryScreen.gameObject.transform);
            containerGO.transform.localScale = Vector3.one;
            containerGO.SetActive(false);
            var component = containerGO.AddComponent<QuickWeaponRackComponent>();

            // setup tab
            var tab = buttonGO.GetComponent<Tab>();
            tab.UpdateVisual(false);
            tab.OnSelectionChanged += (_, selected) => {
                if (selected)
                {
                    tab.Select();
                    containerGO.SetActive(true);
                    component.Show(() => tab.Deselect());
                }
            };

            return component;
        }

        public void Awake()
        {
            var closeButton = _chatScreenCloseButtonField.GetValue(MonoBehaviourSingleton<CommonUI>.Instance.ChatScreen) as Button;
            _closeButtonTemplate = closeButton.gameObject;
        }

        public void Show(Action onClosed)
        {
            // save callback
            _onClosed = onClosed;

            // create and show the grid view
            CreateGridView(_lastArea);

            // add to item context input handler tree, this enables esc handling by callback
            ItemUiContextChildren.Add(this);
        }

        public override void Close()
        {
            // remove from item context input handler tree
            ItemUiContextChildren.Remove(this);

            _windowAnchorPosition = _windowTransform.anchoredPosition;

            _gridView.Close();
            _gridView.Dispose();
            Destroy(_gridView.gameObject);

            _gridView = null;
            _windowTransform = null;
            _activeContainer = null;

            // close callback provided on show
            if (_onClosed != null)
            {
                _onClosed();
            }

            base.Close();
        }

        // is this method too long, yeah, oh well
        private void CreateGridView(EFT.EAreaType initialArea)
        {
            var weaponStand = Plugin.PlayerInventory.HideoutAreaStashes[initialArea];

            // grid view setup, have to create a new grid view each time, since we might upgrade the hideout
            _gridView = ContainedGridsView.CreateGrids(weaponStand, null);
            _gridView.transform.SetParent(gameObject.transform, false);
            _gridView.gameObject.SetActive(true);
            ShowAreaGrid(initialArea);

            _windowTransform = _gridView.transform.Find("ItemInfoWindowTemplate") as RectTransform;

            // remove sort button
            Destroy(_gridView.transform.Find("Sort button").gameObject);

            // hook up tabs
            // yes, code duplication is bad, yes, for this I don't care right now
            var tabTransform = _windowTransform.Find("Tabs");
            var tab1 = tabTransform.Find("Zone 1").gameObject.GetComponent<Tab>();
            var tab2 = tabTransform.Find("Zone 2").gameObject.GetComponent<Tab>();

            tab1.gameObject.SetActive(true);
            tab2.gameObject.SetActive(true);

            tab1.OnSelectionChanged += (_, selected) => {
                if (selected)
                {
                    tab1.Select();
                    tab2.Deselect();
                    ShowAreaGrid(EFT.EAreaType.WeaponStand);
                }
            };

            tab2.OnSelectionChanged += (_, selected) => {
                if (selected)
                {
                    tab1.Deselect();
                    tab2.Select();
                    ShowAreaGrid(EFT.EAreaType.WeaponStandSecondary);
                }
            };

            if (initialArea == EFT.EAreaType.WeaponStand)
            {
                tab1.UpdateVisual(true);
                tab2.UpdateVisual(false);
            }
            else if (initialArea == EFT.EAreaType.WeaponStandSecondary)
            {
                tab1.UpdateVisual(false);
                tab2.UpdateVisual(true);
            }

            // add close button
            var closeButton = Instantiate(_closeButtonTemplate);
            closeButton.GetComponent<LayoutElement>().ignoreLayout = true; // this allows to place close button where we want
            closeButton.transform.SetParent(_windowTransform, false);
            closeButton.GetComponent<Button>().onClick.AddListener(Close);

            RectTransform.anchoredPosition = _windowAnchorPosition;
            CorrectPosition();

            // need to wait for layout to finish before dynamically moving the close button
            // would it be better to learn the layout tools in unity? probably
            this.WaitFrames(1, () => closeButton.RectTransform().anchoredPosition = new Vector2(4, _windowTransform.rect.height / 2 + 13));
        }

        private void ShowAreaGrid(EFT.EAreaType newArea)
        {
            if (_gridView == null)
            {
                return;
            }

            _gridView.Close();

            _activeContainer = Plugin.PlayerInventory.HideoutAreaStashes[newArea];
            var itemContext = new GClass2834(_activeContainer, GClass2834.EItemType.AreaStash, Plugin.PlayerInventory.FavoriteItemsStorage);
            var inventoryController = _itemUiContextInventoryControllerField.GetValue(ItemUiContext.Instance) as InventoryControllerClass;

            _gridView.Show(_activeContainer, itemContext, inventoryController, null, ItemUiContext.Instance, false);
            _lastArea = newArea;
        }

        public override ETranslateResult TranslateCommand(ECommand command)
        {
            if (command != ECommand.Escape || _gridView == null)
			{
				return ETranslateResult.Ignore;
			}

			Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuEscape);
            Close();
			return ETranslateResult.Block;
        }

        public override void TranslateAxes(ref float[] axes)
        {
            // do nothing
        }

        public override ECursorResult ShouldLockCursor()
        {
            return ECursorResult.ShowCursor;
        }
    }
}
