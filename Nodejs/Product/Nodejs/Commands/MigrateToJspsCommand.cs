using EnvDTE;
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudioTools;
using Command = Microsoft.VisualStudioTools.Command;
using Microsoft.NodejsTools.Project;
using MigrateToJsps;
using System.IO;
using System.Linq;
using System.Collections;
using Microsoft.Build.Utilities;

namespace Microsoft.NodejsTools.Commands
{
    internal class MigrateToJspsCommand : Command
    {
        public override int CommandId => (int)PkgCmdId.cmdidJspsProjectMigrate;

        public override async void DoCommand(object sender, EventArgs args)
        {
            Array activeProjects = (Array)NodejsPackage.Instance.DTE.ActiveSolutionProjects;
            EnvDTE.Project project = (EnvDTE.Project)activeProjects.GetValue(0);

            string projectFilepath = project.FullName;

            var nodeProject = (NodejsProjectNode)project.Object;
            string parentProjectDir = Path.GetDirectoryName(nodeProject.ProjectFolder);

            project.Save();
            NodejsPackage.Instance.DTE.Solution.Remove(project);

            JoinableTask<string> newProjectMigration = ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                return MigrationLibrary.Migrate(projectFilepath, parentProjectDir);
            });

            var newProjectFilePath = await newProjectMigration;

            if (newProjectFilePath != null)
            {
                NodejsPackage.Instance.DTE.Solution.AddFromFile(newProjectFilePath, false);
            }
            else
            {
                // put old projectfile back?
            }
        }
    }
}
