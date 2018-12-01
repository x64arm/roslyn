// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpErrorListDesktop : CSharpErrorListCommon
    {
        public CSharpErrorListDesktop() : base(WellKnownProjectTemplates.ClassLibrary) { }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/18996"), TestCategory(Traits.Features.ErrorList)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/18996"), TestCategory(Traits.Features.ErrorList)]
        public override void ErrorLevelWarning()
        {
            base.ErrorLevelWarning();
        }

        [TestMethod, TestCategory(Traits.Features.ErrorList)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
