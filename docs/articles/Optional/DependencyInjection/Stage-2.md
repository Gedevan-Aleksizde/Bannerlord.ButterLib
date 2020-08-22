This is the intermediate stage when SubModules register their services.  
  
Because the [``IServiceProvider``](xref:System.IServiceProvider) is not yet available, it is not possible to the the full [``ILogger``](xref:Microsoft.Extensions.Logging.ILogger) implementation at this stage.  
You can call [``MBSubModuleBase.GetTempServiceProvider()``](xref:Bannerlord.ButterLib.Common.Extensions.DependencyInjectionExtensions.html#collapsible-Bannerlord_ButterLib_Common_Extensions_DependencyInjectionExtensions_GetTempServiceProvider_TaleWorlds_MountAndBlade_MBSubModuleBase_) to get the temporary [``IServiceProvider``](xref:System.IServiceProvider) and resolve [``ILogger``](xref:Microsoft.Extensions.Logging.ILogger) while logging anything at this stage.  
Do not forget to replace your [``ILogger``](xref:Microsoft.Extensions.Logging.ILogger) at Stage 3.  