﻿using Bannerlord.BUTR.Shared.Helpers;

using HarmonyLib;

using NUnit.Framework;

using System.Runtime.CompilerServices;

using TaleWorlds.Library;

namespace Bannerlord.ButterLib.Tests
{
    public class ApplicationVersionUtilsTests
    {
        private static readonly string TestAppVersionStr = "e1.4.3.231432";
        private static readonly ApplicationVersion TestAppVersion = ApplicationVersion.FromString("e1.4.3.231432");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool MockedGetVersionStr(ref string __result)
        {
            __result = TestAppVersionStr;
            return false;
        }

        [SetUp]
        public void Setup()
        {
            var harmony = new Harmony($"{nameof(ApplicationVersionUtilsTests)}.{nameof(Setup)}");
            harmony.Patch(SymbolExtensions.GetMethodInfo(() => ApplicationVersionHelper.GameVersionStr()),
                prefix: new HarmonyMethod(DelegateHelper.GetMethodInfo(MockedGetVersionStr)));
        }

        [Test]
        public void TryParse_Test()
        {
            var result = ApplicationVersionHelper.TryParse(TestAppVersionStr, out var gameVersion);
            Assert.True(result);
            Assert.AreEqual(TestAppVersion, gameVersion);
        }
    }
}