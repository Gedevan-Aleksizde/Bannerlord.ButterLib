[``SaveSystem``](xref:Bannerlord.ButterLib.SaveSystem) provides helper methods for the game's save system.   

## SyncDataAsJson
The current issue with the game's save system is that once using a custom class when saving the game, the save won't be loaded without the custom class.  
We finally have a good solution for this problem. Instead of using the game's serialization logic, we serialize the class ourselves and store it as one of the pre-defined data primitives - string.  
Newtonsoft.Json is used for serializing the class into the string and vise versa. We use a custom contract resolver to detect [``SaveableField``](xref:TaleWorlds.SaveSystem.SaveableFieldAttribute ) and [``SaveableProperty``](xref:TaleWorlds.SaveSystem.SaveablePropertyAttribute ) and serialize the actual data.  
```csharp
public class CustomBehavior : CampaignBehaviorBase
{
    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncDataAsJson("_descriptorManager", ref _descriptorManager);
    }
    public override void RegisterEvents() { }
}
```
With this extension method you will be able to use the game's save system to store optional data that will not break the game once your mod is removed!