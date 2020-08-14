﻿using Bannerlord.ButterLib.CampaignIdentifier;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

using System;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.ButterLib
{
    public class SubModule : LoaderSubModule
    {
        private static IServiceProvider GlobalServiceProvider { get; set; } = default!;
        private static IServiceProvider? GameScopeServiceProvider => GameScope?.ServiceProvider;
        private static IServiceScope? GameScope{ get; set; }

        internal static IServiceProvider ServiceProvider => GameScopeServiceProvider ?? GlobalServiceProvider;
        internal static IServiceCollection Services { get; set; } = default!;

        protected override void OnSubModuleLoad()
        {
            Services = new ServiceCollection();
            var log = new LoggerConfiguration()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(log, dispose: true));
            //Services.AddLogging(builder => builder.AddFile("app.log", append: true));

            base.OnSubModuleLoad();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            GlobalServiceProvider = Services.BuildServiceProvider();
            Services = null!;
            
            var logger = ServiceProvider.GetRequiredService<ILogger<SubModule>>();
            logger.LogError("test");

            base.OnBeforeInitialModuleScreenSetAsRoot();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            GameScope = ServiceProvider.CreateScope();

            if (game.GameType is Campaign)
            {
                //Events
                CampaignIdentifierEvents.Instance = new CampaignIdentifierEvents();
            }

            base.OnGameStart(game, gameStarterObject);
        }

        public override void OnGameEnd(Game game)
        {
            GameScope = null;

            base.OnGameEnd(game);
        }
    }
}