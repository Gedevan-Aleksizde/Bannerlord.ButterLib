﻿using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;

using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.DelayedSubModule
{
    /// <summary>
    /// Allows you to inject your own code into the load / unload sequence
    /// of other modules without specifying them directly as a dependency
    /// in 'SubModule.xml'.
    /// </summary>
    /// <remarks>
    /// Supported methods are: 'OnSubModuleLoad', 'OnSubModuleUnloaded',
    /// 'OnBeforeInitialModuleScreenSetAsRoot', 'OnGameStart', 'OnGameEnd'.
    /// The basic loading order still applies, so while you can technically
    /// subscribe to the 'OnSubModuleLoad' methods of the already loaded modules,
    /// it won't affect them as they will be executed before. The delegate passed
    /// will still be executed.
    /// </remarks>
    public static class DelayedSubModuleManager
    {
        // We need a ConcurrentHashSet
        private static ConcurrentDictionary<Key, object?> RegisteredTypes { get; } = new();

        /// <summary>
        /// An event that is raised when the load / unload methods of the
        /// <see cref="MBSubModuleBase"/> or its derived classes are called.
        /// </summary>
        /// <remarks><para>
        /// Allows you to inject your own code into the load / unload sequence
        /// of other modules without specifying them directly as a dependency
        /// in 'SubModule.xml'.
        /// </para><para>
        /// Supported methods are: 'OnSubModuleLoad', 'OnSubModuleUnloaded',
        /// 'OnBeforeInitialModuleScreenSetAsRoot', 'OnGameStart', 'OnGameEnd'.
        /// </para></remarks>
        private static event EventHandler<SubscriptionGlobalEventArgs>? OnMethod;

        static DelayedSubModuleManager()
        {
            var harmony = new Harmony("butterlib.delayedsubmoduleloader.static");
            harmony.Patch(
                AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnSubModuleLoad"),
                postfix: new HarmonyMethod(typeof(DelayedSubModuleManager), nameof(BaseSubModuleLoadPostfix)));
            harmony.Patch(
                AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnSubModuleUnloaded"),
                postfix: new HarmonyMethod(typeof(DelayedSubModuleManager), nameof(BaseOnSubModuleUnloadedPostfix)));
            harmony.Patch(
                AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnBeforeInitialModuleScreenSetAsRoot"),
                postfix: new HarmonyMethod(typeof(DelayedSubModuleManager), nameof(BaseOnBeforeInitialModuleScreenSetAsRootPostfix)));
            harmony.Patch(
                AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnGameStart"),
                postfix: new HarmonyMethod(typeof(DelayedSubModuleManager), nameof(BaseOnGameStartPostfix)));
            harmony.Patch(
                AccessTools2.DeclaredMethod("TaleWorlds.MountAndBlade.MBSubModuleBase:OnGameEnd"),
                postfix: new HarmonyMethod(typeof(DelayedSubModuleManager), nameof(BaseOnGameEndPostfix)));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BaseSubModuleLoadPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), true, SubscriptionType.AfterMethod, "OnSubModuleLoad"));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BaseOnSubModuleUnloadedPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), true, SubscriptionType.AfterMethod, "OnSubModuleUnloaded"));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BaseOnBeforeInitialModuleScreenSetAsRootPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), true, SubscriptionType.AfterMethod, "OnBeforeInitialModuleScreenSetAsRoot"));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BaseOnGameStartPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), true, SubscriptionType.AfterMethod, "OnGameStart"));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BaseOnGameEndPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), true, SubscriptionType.AfterMethod, "OnGameEnd"));
        }

        /// <summary>Registers a module to be a target of the <see cref="DelayedSubModuleManager"/>.</summary>
        /// <typeparam name="TSubModule">The exact type of the SubModule to be registered.</typeparam>
        public static void Register<TSubModule>() where TSubModule : MBSubModuleBase => Register<TSubModule>(-1, null, null);

        /// <summary>Registers a module to be a target of the <see cref="DelayedSubModuleManager"/>.</summary>
        /// <param name="subModule">The exact type of the SubModule to be registered.</param>
        public static void Register(Type subModule) => Register(subModule, -1, null, null);

        /// <summary>Registers a module to be a target of the <see cref="DelayedSubModuleManager"/> Harmony patching.</summary>
        /// <typeparam name="TSubModule">The exact type of the SubModule to be patched.</typeparam>
        /// <param name="priority">The <see cref="HarmonyPriority"/> that would be assigned to the patches that would be made.</param>
        /// <param name="before">A list of <see cref="Harmony.Id"/>s that should come after the patches that would be made.</param>
        /// <param name="after">A list of <see cref="Harmony.Id"/>s that should come before the patches that would be made.</param>
        public static void Register<TSubModule>(int priority, string[]? before, string[]? after) where TSubModule : MBSubModuleBase => Register(typeof(TSubModule), priority, before, after);

        /// <summary>Registers a module to be a target of the <see cref="DelayedSubModuleManager"/> Harmony patching.</summary>
        /// <param name="subModule">The exact type of the SubModule to be patched.</param>
        /// <param name="priority">The <see cref="HarmonyPriority"/> that would be assigned to the patches that would be made.</param>
        /// <param name="before">A list of <see cref="Harmony.Id"/>s that should come after the patches that would be made.</param>
        /// <param name="after">A list of <see cref="Harmony.Id"/>s that should come before the patches that would be made.</param>
        public static void Register(Type subModule, int priority, string[]? before, string[]? after)
        {
            var key = new Key(subModule, priority, before, after);
            if (!RegisteredTypes.TryAdd(key, null))
                return;

            var harmony = new Harmony($"butterlib.delayedsubmoduleloader.{subModule.Name.ToLowerInvariant()}");
            var onSubModuleLoad = AccessTools2.DeclaredMethod(subModule, "OnSubModuleLoad");
            if (onSubModuleLoad is not null)
            {
                harmony.Patch(
                    onSubModuleLoad,
                    prefix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:SubModuleLoadPrefix"), priority, before, after),
                    postfix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:SubModuleLoadPostfix"), priority, before, after));
            }


            var onSubModuleUnloaded = AccessTools2.DeclaredMethod(subModule, "OnSubModuleUnloaded");
            if (onSubModuleUnloaded is not null)
            {
                harmony.Patch(
                    AccessTools2.DeclaredMethod(subModule, "OnSubModuleUnloaded"),
                    prefix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:OnSubModuleUnloadedPrefix"), priority, before, after),
                    postfix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:OnSubModuleUnloadedPostfix"), priority, before, after));
            }


            var onBeforeInitialModuleScreenSetAsRoot = AccessTools2.DeclaredMethod(subModule, "OnBeforeInitialModuleScreenSetAsRoot");
            if (onBeforeInitialModuleScreenSetAsRoot is not null)
            {
                harmony.Patch(
                    AccessTools2.DeclaredMethod(subModule, "OnBeforeInitialModuleScreenSetAsRoot"),
                    prefix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:OnBeforeInitialModuleScreenSetAsRootPrefix"), priority, before, after),
                    postfix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:OnBeforeInitialModuleScreenSetAsRootPostfix"), priority, before, after));
            }

            var onGameStart = AccessTools2.DeclaredMethod(subModule, "OnGameStart");
            if (onGameStart is not null)
            {
                harmony.Patch(
                    AccessTools2.DeclaredMethod(subModule, "OnGameStart"),
                    prefix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:OnGameStartPrefix"), priority, before, after),
                    postfix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:OnGameStartPostfix"), priority, before, after));
            }


            var onGameEnd = AccessTools2.DeclaredMethod(subModule, "OnGameEnd");
            if (onGameEnd is not null)
            {
                harmony.Patch(
                    AccessTools2.DeclaredMethod(subModule, "OnGameEnd"),
                    prefix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:OnGameEndPrefix"), priority, before, after),
                    postfix: new HarmonyMethod(AccessTools2.DeclaredMethod("Bannerlord.ButterLib.DelayedSubModule.DelayedSubModuleManager:OnGameEndPostfix"), priority, before, after));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SubModuleLoadPrefix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.BeforeMethod, "OnSubModuleLoad"));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SubModuleLoadPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.AfterMethod, "OnSubModuleLoad"));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnSubModuleUnloadedPrefix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.BeforeMethod, "OnSubModuleUnloaded"));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnSubModuleUnloadedPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.AfterMethod, "OnSubModuleUnloaded"));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnBeforeInitialModuleScreenSetAsRootPrefix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.BeforeMethod, "OnBeforeInitialModuleScreenSetAsRoot"));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnBeforeInitialModuleScreenSetAsRootPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.AfterMethod, "OnBeforeInitialModuleScreenSetAsRoot"));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnGameStartPrefix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.BeforeMethod, "OnGameStart"));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnGameStartPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.AfterMethod, "OnGameStart"));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnGameEndPrefix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.BeforeMethod, "OnGameEnd"));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnGameEndPostfix(MBSubModuleBase __instance)
        {
            OnMethod?.Invoke(null, new SubscriptionGlobalEventArgs(__instance.GetType(), false, SubscriptionType.AfterMethod, "OnGameEnd"));
        }

        public static void Subscribe(Type mbSubModuleType, MBSubModuleBase caller, string method, SubscriptionType subscriptionType, EventHandler<SubscriptionEventArgs> @delegate)
        {
            if (!mbSubModuleType.IsSubclassOf(typeof(MBSubModuleBase)))
                return;

            Subscribe(mbSubModuleType, caller.GetType(), method, subscriptionType, @delegate);
        }
        public static void Subscribe<T1, T2>(string method, SubscriptionType subscriptionType, EventHandler<SubscriptionEventArgs> @delegate)
            where T1 : MBSubModuleBase
            where T2 : MBSubModuleBase
        {
            Subscribe(typeof(T1), typeof(T2), method, subscriptionType, @delegate);
        }
        public static void Subscribe<T>(MBSubModuleBase caller, string method, SubscriptionType subscriptionType, EventHandler<SubscriptionEventArgs> @delegate)
            where T : MBSubModuleBase
        {
            Subscribe(typeof(T), caller.GetType(), method, subscriptionType, @delegate);
        }
        private static void Subscribe(Type mbSubModuleType, Type caller, string method, SubscriptionType subscriptionType, EventHandler<SubscriptionEventArgs> @delegate)
        {
            var loadedModules = BUTR.Shared.Helpers.ModuleInfoHelper.GetLoadedModules().ToList();
            var callerModule = BUTR.Shared.Helpers.ModuleInfoHelper.GetModuleByType(caller);
            var destModule = BUTR.Shared.Helpers.ModuleInfoHelper.GetModuleByType(mbSubModuleType);

            var callerModulePosition = loadedModules.IndexOf(callerModule!);
            var destModulePosition = loadedModules.IndexOf(destModule!);

            if (callerModulePosition < destModulePosition) // Dest module was still not called
            {
                OnMethod += (s, e) =>
                {
                    if (!e.IsValid(mbSubModuleType, method, subscriptionType))
                        return;

                    @delegate.Invoke(s, new SubscriptionEventArgs(e.IsBase));
                };
            }
            if (callerModulePosition == destModulePosition) // This should not happen
            {
                if (subscriptionType == SubscriptionType.BeforeMethod)
                {
                    @delegate.Invoke(null, new SubscriptionEventArgs(false));
                }
                if (subscriptionType == SubscriptionType.AfterMethod)
                {
                    OnMethod += (s, e) =>
                    {
                        if (!e.IsValid(mbSubModuleType, method, subscriptionType))
                            return;

                        @delegate.Invoke(s, new SubscriptionEventArgs(e.IsBase));
                    };
                }
            }
            if (callerModulePosition > destModulePosition) // Dest module was already called
            {
                @delegate.Invoke(null, new SubscriptionEventArgs(false));
            }
        }

        private record Key(Type Type, int Priority, string[]? Before, string[]? After);
    }
}