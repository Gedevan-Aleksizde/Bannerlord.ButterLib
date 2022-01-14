﻿using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ButterLib.Common.Extensions;

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using TaleWorlds.CampaignSystem;

namespace Bannerlord.ButterLib.Implementation.SaveSystem.Patches
{
    /// <summary>
    /// Fixes TW's new implementation. They plan to switch namespaces, to Type.FullName will break saves.
    /// Instead, only TW will use Type.Name, the modding community will use Type.FullName
    /// </summary>
    internal sealed class BehaviourNamePatch
    {
        internal static bool Enable(Harmony harmony)
        {
            return harmony.TryPatch(
                AccessTools2.Constructor(typeof(CampaignBehaviorBase)),
                postfix: AccessTools2.Method(typeof(BehaviourNamePatch), nameof(CampaignBehaviorBaseCtorPostfix)));
        }

        internal static bool Disable(Harmony harmony)
        {
            harmony.Unpatch(
                AccessTools2.Constructor(typeof(CampaignBehaviorBase)),
                AccessTools2.Method(typeof(BehaviourNamePatch), nameof(CampaignBehaviorBaseCtorPostfix)));

            return true;
        }

        private static void CampaignBehaviorBaseCtorPostfix(CampaignBehaviorBase instance, ref string ___StringId)
        {
            var module = ModuleInfoHelper.GetModuleByType(instance.GetType());
            if (module is null) // A non-module dll
            {
                return;
            }

            if (module.IsOfficial) // A TW module
            {
                return;
            }

            ___StringId = instance.GetType().FullName;
        }
    }
}