# Bannerlord.ButterLib
<p align="center">
  <a href="https://github.com/BUTR/Bannerlord.ButterLib" alt="Logo">
    <img src="https://github.com/BUTR/Bannerlord.ButterLib/blob/dev/resources/Butter.png?raw=true" />
  </a>
  </br>
  <a href="https://github.com/BUTR/Bannerlord.ButterLib" alt="Lines Of Code">
    <img src="https://aschey.tech/tokei/github/BUTR/Bannerlord.ButterLib?category=code" />
  </a>
  <a href="https://www.codefactor.io/repository/github/butr/bannerlord.butterlib">
    <img src="https://www.codefactor.io/repository/github/butr/bannerlord.butterlib/badge" alt="CodeFactor" />
  </a>
  <a href="https://codeclimate.com/github/BUTR/Bannerlord.ButterLib/maintainability">
    <img alt="Code Climate maintainability" src="https://img.shields.io/codeclimate/maintainability-percentage/BUTR/Bannerlord.ButterLib">
  </a>
  <a href="https://butr.github.io/Bannerlord.ButterLib" alt="Documentation">
    <img src="https://img.shields.io/badge/Documentation-%F0%9F%94%8D-blue?style=flat" />
  </a>
  <a title="Crowdin" target="_blank" href="https://crowdin.com/project/butterlib">
    <img src="https://badges.crowdin.net/butterlib/localized.svg">
  </a>
  </br>
  <a href="https://github.com/BUTR/Bannerlord.ButterLib/actions/workflows/test.yml?query=branch%3Adev">
    <img alt="GitHub Workflow Status (event)" src="https://img.shields.io/github/actions/workflow/status/BUTR/Bannerlord.ButterLib/test.yml?branch=dev&label=Game%20Stable%20and%20Beta">
  </a>
  <a href="https://github.com/BUTR/Bannerlord.ButterLib/actions/workflows/test-full.yml?query=branch%3Adev">
    <img alt="GitHub Workflow Status (event)" src="https://img.shields.io/github/actions/workflow/status/BUTR/Bannerlord.ButterLib/test-full.yml?branch=dev&label=Supported%20Game%20Versions">
  </a>
  <a href="https://codecov.io/gh/BUTR/Bannerlord.ButterLib">
    <img src="https://codecov.io/gh/BUTR/Bannerlord.ButterLib/branch/dev/graph/badge.svg" />
  </a>
  </br>
  <a href="https://www.nuget.org/packages/Bannerlord.ButterLib" alt="NuGet Bannerlord.ButterLib">
  <img src="https://img.shields.io/nuget/v/Bannerlord.ButterLib.svg?label=NuGet%20Bannerlord.ButterLib&colorB=blue" />
  </a>
  </br>
  <a href="https://www.nexusmods.com/mountandblade2bannerlord/mods/2018" alt="NexusMods ButterLib">
    <img src="https://img.shields.io/badge/NexusMods-ButterLib-yellow.svg" />
  </a>
  <a href="https://www.nexusmods.com/mountandblade2bannerlord/mods/2018" alt="NexusMods ButterLib">
    <img src="https://img.shields.io/endpoint?url=https%3A%2F%2Fnexusmods-version-pzk4e0ejol6j.runkit.sh%3FgameId%3Dmountandblade2bannerlord%26modId%3D2018" />
  </a>
  <a href="https://www.nexusmods.com/mountandblade2bannerlord/mods/2018" alt="NexusMods ButterLib">
    <img src="https://img.shields.io/endpoint?url=https%3A%2F%2Fnexusmods-downloads-ayuqql60xfxb.runkit.sh%2F%3Ftype%3Dunique%26gameId%3D3174%26modId%3D2018" />
  </a>
  <a href="https://www.nexusmods.com/mountandblade2bannerlord/mods/2018" alt="NexusMods ButterLib">
    <img src="https://img.shields.io/endpoint?url=https%3A%2F%2Fnexusmods-downloads-ayuqql60xfxb.runkit.sh%2F%3Ftype%3Dtotal%26gameId%3D3174%26modId%3D2018" />
  </a>
  <a href="https://www.nexusmods.com/mountandblade2bannerlord/mods/2018" alt="NexusMods ButterLib">
    <img src="https://img.shields.io/endpoint?url=https%3A%2F%2Fnexusmods-downloads-ayuqql60xfxb.runkit.sh%2F%3Ftype%3Dviews%26gameId%3D3174%26modId%3D2018" />
  </a>
  </br>
</p>

Extension library for Mount & Blade II: Bannerlord.

## Highlighted features:
* [CampaignIdentifier](https://butr.github.io/Bannerlord.ButterLib/articles/CampaignIdentifier/Overview.html) - Associates unique string ID with every campaign based on the initial character.  
* [DistanceMatrix](https://butr.github.io/Bannerlord.ButterLib/articles/DistanceMatrix/Overview.html) - A generic class that pairs given objects of type MBObject and for each pair calculates the distance between the objects that formed it.  
* [DelayedSubModule](https://butr.github.io/Bannerlord.ButterLib/articles/DelayedSubModule/Overview.html) - Execute code after specific SubModule method.  
* [SubModuleWrappers](https://butr.github.io/Bannerlord.ButterLib/articles/SubModuleWrappers/Overview.html) - Wraps MBSubModulebase for easier calling of protected internal metods. 
* [SaveSystem](https://butr.github.io/Bannerlord.ButterLib/articles/SaveSystem/Overview.html) - Provides helper methods for the game's save system.
* [AccessTools2](https://butr.github.io/Bannerlord.ButterLib/api/Bannerlord.ButterLib.Common.Helpers.AccessTools2.html) - Adds delegate related functions.  
* [SymbolExtensions2](https://butr.github.io/Bannerlord.ButterLib/api/Bannerlord.ButterLib.Common.Helpers.SymbolExtensions2.html) - More lambda functions. 
* [AlphanumComparatorFast](https://butr.github.io/Bannerlord.ButterLib/api/Bannerlord.ButterLib.Common.Helpers.AlphanumComparatorFast.html) - Alphanumeric sort. This sorting algorithm logically handles numbers in strings.  

Check the [/Articles](https://butr.github.io/Bannerlord.ButterLib/articles/index.html) section in the documentation to see all available features!

## Installation
### Players
This module should be one of the highest in loading order. Ideally, it should be second in load order after ``Bannerlord.Harmony``.
### Developers
Add this to your `.csproj`. Please note that `IncludeAssets="compile"` is very important!
```xml
  <ItemGroup>
    <PackageReference Include="Bannerlord.ButterLib" Version="1.0.31" IncludeAssets="compile" />
  </ItemGroup>
```

## For Players
This mod is a dependency mod that does not provide anything by itself. You need to additionaly install mods that use it.
