using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace QuickWeaponRackAccess.Patches
{
    internal class InventoryScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(InventoryScreen), x =>
                x.Name == nameof(InventoryScreen.Show) && x.GetParameters()[0].Name == "healthController");
        }

        [PatchPostfix]
        public static void PatchPostfix(InventoryScreen __instance)
        {
            Plugin.Instance.TryAttachToInventoryScreen(__instance);
        }
    }
}
