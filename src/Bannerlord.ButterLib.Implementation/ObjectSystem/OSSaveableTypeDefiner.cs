﻿using System.Collections.Generic;

using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.Implementation.ObjectSystem
{
    internal class OSSaveableTypeDefiner : ButterLibSaveableTypeDefiner
    {
        public OSSaveableTypeDefiner() : base(5) { }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<MBObjectBase>));
        }
    }
}