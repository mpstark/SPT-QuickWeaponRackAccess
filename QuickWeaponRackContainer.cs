using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT.InputSystem;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace QuickWeaponRackAccess
{
    public class QuickWeaponRackComponent : UIInputNode
    {
        private static FieldInfo ItemUiContextInventoryControllerField = AccessTools.GetDeclaredFields(typeof(ItemUiContext)).Single(x => x.FieldType == typeof(InventoryControllerClass));
        private static FieldInfo InputNodeAbstractChildrenField = AccessTools.Field(typeof(InputNodeAbstract), "_children");

        public override RectTransform RectTransform => _windowTransform;

        public LootItemClass ActiveWeaponRackContainer => _activeContainer;

        private List<InputNode> ItemUiContextChildren => InputNodeAbstractChildrenField.GetValue(ItemUiContext.Instance) as List<InputNode>;

        private LootItemClass _activeContainer;
        private ContainedGridsView _gridView;
        private RectTransform _windowTransform;
        private Vector2 _windowAnchorPosition = new(360, 315);
        private GameObject _closeButtonTemplate;
        private Action _onClosed;
        private EFT.EAreaType _lastArea = EFT.EAreaType.WeaponStand;


        public void Awake()
        {
            _closeButtonTemplate = GameObject.Find("Common UI/Common UI/ChatScreen/Content/ChatPanel/ChatPart/CaptionPanel/CloseButton");
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
            // yes, code depublication is bad, yes, for this I don't care right now
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
            var itemContext = new GClass2818(_activeContainer, GClass2818.EItemType.AreaStash, Plugin.PlayerInventory.FavoriteItemsStorage);
            var inventoryController = ItemUiContextInventoryControllerField.GetValue(ItemUiContext.Instance) as InventoryControllerClass;

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
