using System.Collections.Generic;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using HarmonyLib;

namespace QuickWeaponRackAccess.Patches
{
    internal class QuickFindAppropriatePlacePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.QuickFindAppropriatePlace));
        }

        /// <summary>
        /// Allows for quick moving items into the quick access weapon rack window
        /// </summary>
        [PatchPrefix]
        public static void PatchPrefix(Item item, ref IEnumerable<LootItemClass> targets, InteractionsHandlerClass.EMoveItemOrder order)
        {
            // THIS IS BASED OFF OF CODE FROM DRAKIAXYZ, under MIT license
            // If `order` doesn't have `MoveToAnotherSide` set, don't do anything
            if (!order.HasFlag(InteractionsHandlerClass.EMoveItemOrder.MoveToAnotherSide))
            {
                return;
            }

            var rackContainer = Plugin.Instance.QuickWeaponRackComponent?.ActiveWeaponRackContainer;
            if (rackContainer == null || item.Parent.Container.ParentItem.Id == rackContainer.Id)
            {
                return;
            }

            var newTargets = new List<LootItemClass>();
            newTargets.Add(rackContainer);
            newTargets.AddRange(targets);

            targets = newTargets;
        }
    }
}
