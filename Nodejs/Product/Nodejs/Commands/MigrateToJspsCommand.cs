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

namespace Microsoft.NodejsTools.Commands
{
    internal class MigrateToJspsCommand : Command
    {
        public override int CommandId => (int)PkgCmdId.cmdidJspsProjectMigrate;

        public override void DoCommand(object sender, EventArgs args)
        {
            var vsSolution = NodejsPackage.Instance.GetService<SVsSolution, IVsSolution>();
            var vsSolution4 = NodejsPackage.Instance.GetService<SVsSolution, IVsSolution4>();
            var uiShell = NodejsPackage.Instance.GetService<SVsUIShell, IVsUIShell>();

            Array activeProjects = (Array)NodejsPackage.Instance.DTE.ActiveSolutionProjects;
            EnvDTE.Project project = (EnvDTE.Project)activeProjects.GetValue(0);

            string projectFilepath = project.FullName;

            var nodeProject = (NodejsProjectNode)project.Object;
            string parentProjectDir = Path.GetDirectoryName(nodeProject.ProjectFolder);

            project.Save();
            NodejsPackage.Instance.DTE.Solution.Remove(project);

            //ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            //{
            //    MigrationLibrary.Migrate(projectFilepath, parentProjectDir);
            //});
            var newProjectFilePath = MigrationLibrary.Migrate(projectFilepath, parentProjectDir);

            NodejsPackage.Instance.DTE.Solution.AddFromFile(newProjectFilePath, false);

            //Guid emptyGuid = Guid.Empty;
            //Guid newProjectIdentifier = new Guid();
            //int result = vsSolution.CreateProject(ref emptyGuid, newProjectFilePath, null, null, (uint)__VSCREATEPROJFLAGS.CPF_OPENFILE, ref newProjectIdentifier, out IntPtr projectPtr);
            //if (result == 0)
            //{
            //    // yay! do nothing
            //}
            //else
            //{
            //    uiShell.ReportErrorInfo(result);
            //    //NodejsPackage.Instance.DTE.Solution.AddFromFile(newProjectFilePath, false);
            //}

        }
    }
}
