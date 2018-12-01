// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpSquigglesNetCore : CSharpSquigglesCommon
    {
        public CSharpSquigglesNetCore( )
            :base( WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/19091"), TestCategory(Traits.Features.ErrorSquiggles)]
        [TestCategory(Traits.Features.NetCore)]
        public override void VerifySyntaxErrorSquiggles()
        {
            base.VerifySyntaxErrorSquiggles();
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/19091"), TestCategory(Traits.Features.ErrorSquiggles)]
        [TestCategory(Traits.Features.NetCore)]
        public override void VerifySemanticErrorSquiggles()
        {
            base.VerifySemanticErrorSquiggles();
        }
    }
}
