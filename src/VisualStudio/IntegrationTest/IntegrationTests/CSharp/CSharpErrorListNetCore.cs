// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpErrorListNetCore : CSharpErrorListCommon
    {
        public CSharpErrorListNetCore() : base(WellKnownProjectTemplates.CSharpNetCoreClassLibrary) { }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/18996"), TestCategory(Traits.Features.ErrorList)]
        [TestCategory(Traits.Features.NetCore)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/18996"), TestCategory(Traits.Features.ErrorList)]
        [TestCategory(Traits.Features.NetCore)]
        public override void ErrorLevelWarning()
        {
            base.ErrorLevelWarning();
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/19090"), TestCategory(Traits.Features.ErrorList)]
        [TestCategory(Traits.Features.NetCore)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
