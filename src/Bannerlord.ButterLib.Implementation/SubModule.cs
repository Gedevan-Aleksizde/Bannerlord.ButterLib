﻿using Bannerlord.ButterLib.CampaignIdentifier;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.DelayedSubModule;
using Bannerlord.ButterLib.DistanceMatrix;
using Bannerlord.ButterLib.Implementation.CampaignIdentifier;
using Bannerlord.ButterLib.Implementation.CampaignIdentifier.CampaignBehaviors;
using Bannerlord.ButterLib.Implementation.Common.Extensions;
using Bannerlord.ButterLib.Implementation.DistanceMatrix;
using Bannerlord.ButterLib.Implementation.Logging;
using Bannerlord.ButterLib.Implementation.SaveSystem;
using Bannerlord.ButterLib.SaveSystem;

using HarmonyLib;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StoryMode;

using System;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;

namespace Bannerlord.ButterLib.Implementation
{
    public sealed class SubModule : MBSubModuleBase
    {
        private const string SErrorLoading = "{=PZthBmJc9B}ButterLib Campaign Identifier failed to load! See details in the mod log.";

        public Harmony? CampaignIdentifierHarmonyInstance { get; private set; }
        internal bool Patched { get; private set; }
        private bool FirstInit { get; set; } = true;

        internal static ILogger? Logger { get; private set; }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            Logger = this.GetTempServiceProvider().GetRequiredService<ILogger<SubModule>>();
            Logger.LogTrace("OnSubModuleLoad() started tracking.");

            Logger.LogInformation("Wrapping DebugManager of type {type} with DebugManagerWrapper.", Debug.DebugManager.GetType());
            Debug.DebugManager = new DebugManagerWrapper(Debug.DebugManager, this.GetTempServiceProvider()!);

            var services = this.GetServices();
            services.AddScoped<CampaignDescriptor, CampaignDescriptorImplementation>();
            services.AddSingleton<CampaignDescriptorStatic, CampaignDescriptorStaticImplementation>();
            services.AddScoped(typeof(DistanceMatrix<>), typeof(DistanceMatrixImplementation<>));
            services.AddSingleton<DistanceMatrixStatic, DistanceMatrixStaticImplementation>();
            services.AddSingleton<ICampaignExtensions, CampaignExtensionsImplementation>();
            services.AddTransient<ICampaignDescriptorProvider, JsonCampaignDescriptorProvider>();
            services.AddScoped<IMBObjectVariableStorage, MBObjectVariableStorageBehavior>();

            DelayedSubModuleManager.Register<StoryModeSubModule>();
            DelayedSubModuleManager.Subscribe<StoryModeSubModule, SubModule>(
                nameof(OnSubModuleLoad), SubscriptionType.AfterMethod, InitializeCampaignIdentifier);

            Logger.LogTrace("OnSubModuleLoad() finished.");
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            Logger.LogTrace("OnBeforeInitialModuleScreenSetAsRoot() started.");

            if (FirstInit)
            {
                FirstInit = false;

                var serviceProvider = this.GetServiceProvider();
                Logger = serviceProvider.GetRequiredService<ILogger<SubModule>>();

                if (Debug.DebugManager is DebugManagerWrapper debugManagerWrapper)
                {
                    Debug.DebugManager = new DebugManagerWrapper(debugManagerWrapper.OriginalDebugManager, serviceProvider!);
                }
                else
                {
                    Logger.LogWarning("DebugManagerWrapper was replaced with {type}! Wrapping it with DebugManagerWrapper.", Debug.DebugManager.GetType());
                    Debug.DebugManager = new DebugManagerWrapper(Debug.DebugManager, serviceProvider!);
                }

                DelayedSubModuleManager.Register<GauntletUISubModule>();
                DelayedSubModuleManager.Subscribe<GauntletUISubModule, SubModule>(
                    nameof(OnBeforeInitialModuleScreenSetAsRoot), SubscriptionType.AfterMethod, WarnNotPatched);
            }

            Logger.LogTrace("OnBeforeInitialModuleScreenSetAsRoot() finished.");
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            Logger.LogTrace("OnGameStart(Game, IGameStarter) started.");

            if (game.GameType is Campaign)
            {
                CampaignGameStarter gameStarter = (CampaignGameStarter) gameStarterObject;

                // Behaviors
                gameStarter.AddBehavior(new CampaignIdentifierBehavior());
                gameStarter.AddBehavior(new GeopoliticsCachingBehavior());

                // Pseudo-behavior for MBObjectBase extension variable storage
                MBObjectVariableStorageBehavior.Instance = new MBObjectVariableStorageBehavior();
            }
            Logger.LogTrace("OnGameStart(Game, IGameStarter) finished.");
        }


        public override void OnGameEnd(Game game)
        {
            base.OnGameEnd(game);
            Logger.LogTrace("OnGameEnd(Game) started.");

            if (game.GameType is Campaign)
                MBObjectVariableStorageBehavior.Instance = null;

            Logger.LogTrace("OnGameEnd(Game) finished.");
        }

        private void InitializeCampaignIdentifier(object s, SubscriptionEventArgs e)
        {
            if (!e.IsBase)
                return;

            try
            {
                CampaignIdentifierHarmonyInstance ??= new Harmony("Bannerlord.ButterLib.CampaignIdentifier");
                CampaignIdentifierHarmonyInstance.PatchAll();
                Patched = true;
            }
            catch (Exception ex)
            {
                Patched = false;
                Logger.LogError(ex, "Error in OnSubModuleLoad while initializing CampaignIdentifier.");
            }
        }

        private void WarnNotPatched(object s, SubscriptionEventArgs e)
        {
            if (e.IsBase)
                return;

            if (!Patched)
            {
                Logger.LogError("Failed to execute patches!");
                InformationManager.DisplayMessage(new InformationMessage(new TextObject(SErrorLoading).ToString(), Colors.Red));
            }
        }
    }
}