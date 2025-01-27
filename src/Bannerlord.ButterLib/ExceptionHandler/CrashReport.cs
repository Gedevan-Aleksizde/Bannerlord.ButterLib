﻿using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ModuleManager;

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bannerlord.ButterLib.ExceptionHandler
{
    internal record StacktraceEntry(MethodBase Method, bool MethodFromStackframeIssue, ModuleInfoExtended? ModuleInfo, string StackFrameDescription, string[] CilInstructions);

    internal class CrashReport
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Exception Exception { get; }
        public List<StacktraceEntry> Stacktrace { get; }
        public List<ModuleInfoExtendedWithMetadata> LoadedModules { get; } = ModuleInfoHelper.GetLoadedModules().ToList();
        public List<Assembly> ModuleLoadedAssemblies { get; } = new();
        public List<Assembly> ExternalLoadedAssemblies { get; } = new();
        public Dictionary<MethodBase, Patches> LoadedHarmonyPatches { get; } = new();

        public CrashReport(Exception exception)
        {
            Exception = exception;

            Stacktrace = GetAllInvolvedModules(exception).ToList();

            var moduleAssemblies = new List<string>();
            foreach (var subModule in LoadedModules.SelectMany(module => module.SubModules))
            {
                moduleAssemblies.Add(Path.GetFileNameWithoutExtension(subModule.DLLName));
                moduleAssemblies.AddRange(subModule.Assemblies.Select(Path.GetFileNameWithoutExtension).Where(x => x is not null));
            }

            ModuleLoadedAssemblies.AddRange(AccessTools2.AllAssemblies().Where(a => moduleAssemblies.Contains(a.GetName().Name!)));
            ExternalLoadedAssemblies.AddRange(AccessTools2.AllAssemblies().Where(a => !moduleAssemblies.Contains(a.GetName().Name!)));

            foreach (var originalMethod in Harmony.GetAllPatchedMethods())
            {
                var patches = Harmony.GetPatchInfo(originalMethod);
                if (originalMethod is null || patches is null) continue;
                LoadedHarmonyPatches.Add(originalMethod, patches);
            }
        }

        private static IEnumerable<StacktraceEntry> GetAllInvolvedModules(Exception ex)
        {
            static Patches? FindPatches(MethodBase? method) => method is MethodInfo replacement
                ? Harmony.GetOriginalMethod(replacement) is { } original ? Harmony.GetPatchInfo(original) : null
                : null;

            static IEnumerable<(MethodBase, ModuleInfoExtended)> GetPrefixes(Patches? info) => info is null
                ? Enumerable.Empty<(MethodBase, ModuleInfoExtended)>()
                : AddMetadata(info.Prefixes.OrderBy(t => t.priority).Select(t => t.PatchMethod));

            static IEnumerable<(MethodBase, ModuleInfoExtended)> GetPostfixes(Patches? info) => info is null
                ? Enumerable.Empty<(MethodBase, ModuleInfoExtended)>()
                : AddMetadata(info.Postfixes.OrderBy(t => t.priority).Select(t => t.PatchMethod));

            static IEnumerable<(MethodBase, ModuleInfoExtended)> GetTranspilers(Patches? info) => info is null
                ? Enumerable.Empty<(MethodBase, ModuleInfoExtended)>()
                : AddMetadata(info.Transpilers.OrderBy(t => t.priority).Select(t => t.PatchMethod));

            static IEnumerable<(MethodBase, ModuleInfoExtended)> GetFinalizers(Patches? info) => info is null
                ? Enumerable.Empty<(MethodBase, ModuleInfoExtended)>()
                : AddMetadata(info.Finalizers.OrderBy(t => t.priority).Select(t => t.PatchMethod));

            static IEnumerable<(MethodBase, ModuleInfoExtended)> AddMetadata(IEnumerable<MethodInfo> methods)
            {
                foreach (var method in methods)
                {
                    if (method.DeclaringType is { } declaringType && ModuleInfoHelper.GetModuleByType(declaringType) is { } moduleInfo)
                    {
                        yield return (method, moduleInfo);
                    }
                }
            }

            static ModuleInfoExtended? GetModuleInfoIfMod(MethodBase? method) => method?.DeclaringType is not null
                ? ModuleInfoHelper.GetModuleByType(method.DeclaringType)
                : null;


            var inner = ex.InnerException;
            if (inner is not null)
            {
                foreach (var modInfo in GetAllInvolvedModules(inner))
                {
                    yield return modInfo;
                }
            }

            var trace = new EnhancedStackTrace(ex);
            foreach (var frame in trace.GetFrames())
            {
                if (!frame.HasMethod()) continue;

                MethodBase method;
                var methodFromStackframeIssue = false;
                try
                {
                    method = Harmony.GetMethodFromStackframe(frame);
                }
                // NullReferenceException means the method was not found. Harmony doesn't handle this case gracefully
                catch (NullReferenceException)
                {
                    method = frame.GetMethod()!;
                }
                // The given generic instantiation was invalid.
                // From what I understand, this will occur with generic methods
                // Also when static constructors throw errors, Harmony resolution will fail
                catch (Exception)
                {
                    methodFromStackframeIssue = true;
                    method = frame.GetMethod()!;
                }

                var frameDesc = $"{frame} (IL Offset: {frame.GetILOffset()})";

                var patches = FindPatches(method);
                foreach (var (methodBase, extendedModuleInfo) in GetFinalizers(patches))
                {
                    yield return new(methodBase, methodFromStackframeIssue, extendedModuleInfo, frameDesc, Array.Empty<string>());
                }
                foreach (var (methodBase, extendedModuleInfo) in GetPostfixes(patches))
                {
                    yield return new(methodBase, methodFromStackframeIssue, extendedModuleInfo, frameDesc, Array.Empty<string>());
                }
                foreach (var (methodBase, extendedModuleInfo) in GetPrefixes(patches))
                {
                    yield return new(methodBase, methodFromStackframeIssue, extendedModuleInfo, frameDesc, Array.Empty<string>());
                }
                foreach (var (methodBase, extendedModuleInfo) in GetTranspilers(patches))
                {
                    yield return new(methodBase, methodFromStackframeIssue, extendedModuleInfo, frameDesc, Array.Empty<string>());
                }

                var moduleInfo = GetModuleInfoIfMod(method);

                yield return new(method, methodFromStackframeIssue, moduleInfo, frameDesc, Array.Empty<string>());

                /*
                // Further versions of Harmony will do `PlatformTriple.Current.GetIdentifiable(method) is MethodInfo identifiableMethod` themselves
                if (method is MethodInfo && MonoMod.Core.Platforms.PlatformTriple.Current.GetIdentifiable(method) is MethodInfo identifiableMethod && Harmony.GetOriginalMethod(identifiableMethod) is { } original)
                {
                    AsmResolver.DotNet.Code.Cil.CilInstructionCollection? instructions;
                    try
                    {
                        var module = AsmResolver.DotNet.ModuleDefinition.FromModule(identifiableMethod.Module);
                        var dynamicMethodDefinition = new AsmResolver.DotNet.Dynamic.DynamicMethodDefinition(module, identifiableMethod);
                        var cilMethodBody = dynamicMethodDefinition.MethodBody as AsmResolver.DotNet.Code.Cil.CilMethodBody;
                        instructions = cilMethodBody?.Instructions;
                    }
                    catch (Exception)
                    {
                        instructions = null;
                    }

                    var instructionsStr = instructions.Select(x => x.ToString()).ToArray();
                    yield return new(original, methodFromStackframeIssue, moduleInfo, frameDesc, instructionsStr);
                }
                */
            }
        }
    }
}