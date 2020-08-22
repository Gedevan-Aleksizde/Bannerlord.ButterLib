using Bannerlord.ButterLib.Common.Helpers;
using Bannerlord.ButterLib.DelayedSubModule;
using Bannerlord.ButterLib.SubModuleWrappers;

using HarmonyLib;

using NUnit.Framework;

using System;
using System.Collections.Generic;

using TaleWorlds.Library;

namespace Bannerlord.ButterLib.Tests.DelayedSubModule
{
    public class DelayedSubModuleTest
    {
        private static bool MockedGetModuleInfo(Type type, ref ModuleInfo? __result)
        {
            if (type == typeof(TestSubModuleCaller))

            {
                __result = TestHelper.ModuleInfoCaller;
            }
            if (type == typeof(TestSubModuleTarget))
            {
                __result = TestHelper.ModuleInfoTarget;
            }

            return false;
        }

        [SetUp]
        public void Setup()
        {
            var harmony = new Harmony($"{nameof(DelayedSubModuleTest)}.{nameof(Setup)}");
            harmony.Patch(SymbolExtensions.GetMethodInfo(() => ModuleInfoHelper.GetModuleInfo(null!)),
                prefix: new HarmonyMethod(DelegateHelper.GetMethodInfo(MockedGetModuleInfo)));

            DelayedSubModuleManager.Register<TestSubModuleCaller>();
            DelayedSubModuleManager.Register<TestSubModuleTarget>();
        }

        [Test]
        public void SubscribeBeforeTargetLoad_Test()
        {
            // Initialization
            static bool MockedGetLoadedModules(ref List<ModuleInfo> __result)
            {
                __result = new List<ModuleInfo> { TestHelper.ModuleInfoCaller, TestHelper.ModuleInfoTarget };
                return false;
            }

            using var harmony = new HarmonyDisposable($"{nameof(DelayedSubModuleTest)}.{nameof(SubscribeAfterTargetLoad_Test)}");
            harmony.Patch(SymbolExtensions.GetMethodInfo(() => ModuleInfoHelper.GetLoadedModules()),
                prefix: new HarmonyMethod(DelegateHelper.GetMethodInfo(MockedGetLoadedModules)));
            // Initialization

            // Because the Target SubModule loads after Caller, Subscribe should not instacall the delegate
            var delegateWasCalled = false;
            void Delegate(object? sender, SubscriptionEventArgs e) => delegateWasCalled = true;
            DelayedSubModuleManager.Subscribe<TestSubModuleTarget, TestSubModuleCaller>("OnSubModuleLoad", SubscriptionType.AfterMethod, Delegate);
            Assert.False(delegateWasCalled);
        }

        [Test]
        public void SubscribeBeforeTargetLoad_CallTargetManually_Test()
        {
            // Initialization
            static bool MockedGetLoadedModules(ref List<ModuleInfo> __result)
            {
                __result = new List<ModuleInfo> { TestHelper.ModuleInfoCaller, TestHelper.ModuleInfoTarget };
                return false;
            }

            using var harmony = new HarmonyDisposable($"{nameof(DelayedSubModuleTest)}.{nameof(SubscribeBeforeTargetLoad_CallTargetManually_Test)}");
            harmony.Patch(SymbolExtensions.GetMethodInfo(() => ModuleInfoHelper.GetLoadedModules()),
                prefix: new HarmonyMethod(DelegateHelper.GetMethodInfo(MockedGetLoadedModules)));
            // Initialization

            // Because the Target SubModule loads after Caller, Subscribe should not instacall the delegate
            var delegateWasCalled = false;
            void Delegate(object? sender, SubscriptionEventArgs e) => delegateWasCalled = true;
            DelayedSubModuleManager.Subscribe<TestSubModuleTarget, TestSubModuleCaller>("OnSubModuleLoad", SubscriptionType.AfterMethod, Delegate);
            Assert.False(delegateWasCalled);

            // Manually trigger OnSubModuleLoad and confirm that the delegate is now called
            var module = new MBSubModuleBaseWrapper(new TestSubModuleTarget());
            module.SubModuleLoad();
            Assert.True(delegateWasCalled);
        }

        [Test]
        public void SubscribeAfterTargetLoad_Test()
        {
            // Initialization
            static bool MockedGetLoadedModules(ref List<ModuleInfo> __result)
            {
                __result = new List<ModuleInfo> { TestHelper.ModuleInfoTarget, TestHelper.ModuleInfoCaller };
                return false;
            }

            using var harmony = new HarmonyDisposable($"{nameof(DelayedSubModuleTest)}.{nameof(SubscribeAfterTargetLoad_Test)}");
            harmony.Patch(SymbolExtensions.GetMethodInfo(() => ModuleInfoHelper.GetLoadedModules()),
                prefix: new HarmonyMethod(DelegateHelper.GetMethodInfo(MockedGetLoadedModules)));
            // Initialization

            // Because the Target SubModule loads before Caller, Subscribe should instacall the delegate
            var delegateWasCalled = false;
            void Delegate(object? sender, SubscriptionEventArgs e) => delegateWasCalled = true;
            DelayedSubModuleManager.Subscribe<TestSubModuleTarget, TestSubModuleCaller>("OnSubModuleLoad", SubscriptionType.AfterMethod, Delegate);
            Assert.True(delegateWasCalled);
        }
    }
}