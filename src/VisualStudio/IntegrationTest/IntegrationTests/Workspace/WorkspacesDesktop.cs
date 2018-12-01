// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [TestClass]
    public class WorkspacesDesktop : WorkspaceBase
    {
        public WorkspacesDesktop( )
            : base( WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [TestMethod, TestCategory(Traits.Features.Workspace)]
        public override void OpenCSharpThenVBSolution()
        {
            base.OpenCSharpThenVBSolution();
        }

        [TestMethod, TestCategory(Traits.Features.Workspace)]
        public override void MetadataReference()
        {
            base.MetadataReference();
        }

        [TestMethod, TestCategory(Traits.Features.Workspace)]
        public override void ProjectReference()
        {
            base.ProjectReference();
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/19914"), TestCategory(Traits.Features.Workspace)]
        public override void ProjectProperties()
        {
            VisualStudioInstance.SolutionExplorer.CreateSolution(nameof(WorkspacesDesktop));
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            base.ProjectProperties();
        }
    }
}
