﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [ProjectSystemTrait]
    public class SourceItemHandlerTests
    {
        [Fact]
        public void Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new SourceItemHandler(IUnconfiguredProjectFactory.Create(), null));
            Assert.Throws<ArgumentNullException>(() => new SourceItemHandler(null, IPhysicalProjectTreeFactory.Create()));
            new SourceItemHandler(IUnconfiguredProjectFactory.Create(), IPhysicalProjectTreeFactory.Create());
        }

        [Fact]
        public void UniqueSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            Action<string> onSourceFileAdded = s => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            Action<string> onSourceFileRemoved = s => sourceFilesPushedToWorkspace.Remove(s);

            var project = IUnconfiguredProjectFactory.Create(filePath: @"C:\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.Create(project, onSourceFileAdded, onSourceFileRemoved);

            var handler = new SourceItemHandler(project, IPhysicalProjectTreeFactory.Create());
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file2.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null);
            var empty = CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null);

            handler.Handle(added: added, removed: empty, context: context, isActiveContext: true);

            Assert.Equal(2, sourceFilesPushedToWorkspace.Count);
            Assert.Contains(@"C:\file1.cs", sourceFilesPushedToWorkspace);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);

            var removed = CSharpCommandLineParser.Default.Parse(args: new[] { @"C:\file1.cs", @"C:\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null);
            handler.Handle(added: empty, removed: removed, context: context, isActiveContext: true);

            Assert.Equal(1, sourceFilesPushedToWorkspace.Count);
            Assert.Contains(@"C:\file2.cs", sourceFilesPushedToWorkspace);
        }

        [Fact]
        public void RootedSourceFilesPushedToWorkspace()
        {
            var sourceFilesPushedToWorkspace = new HashSet<string>(StringComparers.Paths);
            Action<string> onSourceFileAdded = s => Assert.True(sourceFilesPushedToWorkspace.Add(s));
            Action<string> onSourceFileRemoved = s => sourceFilesPushedToWorkspace.Remove(s);

            var project = IUnconfiguredProjectFactory.Create(filePath: @"C:\ProjectFolder\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.Create(project, onSourceFileAdded, onSourceFileRemoved);

            var handler = new SourceItemHandler(project, IPhysicalProjectTreeFactory.Create());
            var projectDir = Path.GetDirectoryName(project.FullPath);
            var added = CSharpCommandLineParser.Default.Parse(args: new[] { @"file1.cs", @"..\ProjectFolder\file1.cs" }, baseDirectory: projectDir, sdkDirectory: null);
            var removed = CSharpCommandLineParser.Default.Parse(args: new string[] { }, baseDirectory: projectDir, sdkDirectory: null);

            handler.Handle(added: added, removed: removed, context: context, isActiveContext: true);

            Assert.Equal(1, sourceFilesPushedToWorkspace.Count);
            Assert.Contains(@"C:\ProjectFolder\file1.cs", sourceFilesPushedToWorkspace);
        }
    }
}
