﻿using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.DistanceMatrix;
using Bannerlord.ButterLib.HotKeys;
using Bannerlord.ButterLib.Implementation.DistanceMatrix;
using Bannerlord.ButterLib.Implementation.HotKeys;
using Bannerlord.ButterLib.Implementation.Logging;
using Bannerlord.ButterLib.Implementation.MBSubModuleBaseExtended;
using Bannerlord.ButterLib.Implementation.ObjectSystem;
using Bannerlord.ButterLib.Implementation.SaveSystem;
using Bannerlord.ButterLib.ObjectSystem;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.Implementation
{
    public sealed class SubModule : MBSubModuleBase
    {
        internal static ILogger? Logger { get; private set; }
        internal static SubModule? Instance { get; private set; }

        private bool ServiceRegistrationWasCalled { get; set; }
        private bool OnBeforeInitialModuleScreenSetAsRootWasCalled { get; set; }

        public void OnServiceRegistration()
        {
            ServiceRegistrationWasCalled = true;

            if (this.GetServices() is { } services)
            {
                services.AddScoped(typeof(DistanceMatrix<>), typeof(DistanceMatrixImplementation<>));
                services.AddSingleton<IDistanceMatrixStatic, DistanceMatrixStaticImplementation>();

                services.AddScoped<IMBObjectExtensionDataStore, MBObjectExtensionDataStore>();
                services.AddScoped<IMBObjectFinder, MBObjectFinder>();
                services.AddScoped<IMBObjectKeeper, MBObjectKeeper>();

                services.AddScoped<HotKeyManager, HotKeyManagerImplementation>();
                services.AddSingleton<IHotKeyManagerStatic, HotKeyManagerStaticImplementation>();

                services.AddSubSystem<DistanceMatrixSubSystem>();
                services.AddSubSystem<HotKeySubSystem>();
                services.AddSubSystem<MBSubModuleBaseExSubSystem>();
                services.AddSubSystem<ObjectSystemSubSystem>();
                services.AddSubSystem<SaveSystemSubSystem>();
            }
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            Instance = this;
            var serviceProvider = ServiceRegistrationWasCalled ? this.GetServiceProvider() : this.GetTempServiceProvider();

            if (!ServiceRegistrationWasCalled)
                OnServiceRegistration();

            Logger = serviceProvider.GetRequiredService<ILogger<SubModule>>();
            Logger.LogTrace("ButterLib.Implementation: OnSubModuleLoad");

            Logger.LogInformation("Wrapping DebugManager of type {Type} with DebugManagerWrapper", Debug.DebugManager.GetType());
            Debug.DebugManager = new DebugManagerWrapper(Debug.DebugManager, serviceProvider.GetRequiredService<ILoggerFactory>());

            HotKeySubSystem.Instance?.Enable();
            MBSubModuleBaseExSubSystem.Instance?.Enable();
            SaveSystemSubSystem.Instance?.Enable();

            Logger.LogTrace("ButterLib.Implementation: OnSubModuleLoad: Done");
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            Logger.LogTrace("ButterLib.Implementation: OnBeforeInitialModuleScreenSetAsRoot");

            if (!OnBeforeInitialModuleScreenSetAsRootWasCalled)
            {
                OnBeforeInitialModuleScreenSetAsRootWasCalled = true;

                Logger = this.GetServiceProvider().GetRequiredService<ILogger<SubModule>>();

                if (Debug.DebugManager is not DebugManagerWrapper)
                {
                    Logger.LogWarning("DebugManagerWrapper was replaced with {Type}! Wrapping it with DebugManagerWrapper", Debug.DebugManager.GetType());
                    Debug.DebugManager = new DebugManagerWrapper(Debug.DebugManager, this.GetServiceProvider().GetRequiredService<ILoggerFactory>());
                }

                ObjectSystemSubSystem.Instance?.Enable();
            }

            Logger.LogTrace("ButterLib.Implementation: OnBeforeInitialModuleScreenSetAsRoot: Done");
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            Logger.LogTrace("ButterLib.Implementation: OnGameStart");

            if (game.GameType is Campaign)
            {
                var gameStarter = (CampaignGameStarter) gameStarterObject;

                if (DistanceMatrixSubSystem.Instance is { } subSystem)
                {
                    if (subSystem.IsEnabled)
                    {
                        gameStarter.AddBehavior(new GeopoliticsBehavior());
                    }
                    subSystem.GameInitialized = true;
                }
            }

            Logger.LogTrace("ButterLib.Implementation: OnGameStart: Done");
        }

        public override void OnGameEnd(Game game)
        {
            base.OnGameEnd(game);
            Logger.LogTrace("ButterLib.Implementation: OnGameEnd");

            if (game.GameType is Campaign)
            {
                if (DistanceMatrixSubSystem.Instance is { } subSystem)
                {
                    subSystem.GameInitialized = false;
                }
            }

            Logger.LogTrace("ButterLib.Implementation: OnGameEnd: Done");
        }
    }
}