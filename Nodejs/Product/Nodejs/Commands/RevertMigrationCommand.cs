using System;
using Microsoft.NodejsTools.Project;
using Microsoft.NodejsTools.Telemetry;
using System.IO;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudio.Shell;
using System.Linq;

namespace Microsoft.NodejsTools.Commands
{
    internal class RevertMigrationCommand : Command
    {
        public override int CommandId => (int)PkgCmdId.cmdidJspsProjectRevert;

        public override void DoCommand(object sender, EventArgs args)
        {
            EnvDTE.Project project = GetActiveProject();

            string projectFilepath = project.FullName;
            string projectFolder = Path.GetDirectoryName(projectFilepath); 

            string njsprojFile = Directory.GetFiles(projectFolder).Where(file => file.EndsWith(".njsproj")).FirstOrDefault();

            if (njsprojFile == null)
            {
                // log error, show user message
            }
            else
            {
                project.Save();
                NodejsPackage.Instance.DTE.Solution.Remove(project);
                NodejsPackage.Instance.DTE.Solution.AddFromFile(njsprojFile, false);

                if (!NodejsPackage.Instance.DTE.Solution.Saved)
                {
                    var solutionFile = NodejsPackage.Instance.DTE.Solution.FullName;
                    NodejsPackage.Instance.DTE.Solution.SaveAs(solutionFile);
                }

                EnvDTE.Project oldNtvsProject = GetActiveProject();
                var projectGuid = oldNtvsProject.GetNodejsProject().ProjectGuid;

                TelemetryHelper.LogUserRevertedBackToNtvs(projectGuid.ToString());
            }
        }

        public override EventHandler BeforeQueryStatus
        {
            get
            {
                return new EventHandler((sender, args) => {
                    try
                    {
                        EnvDTE.Project activeProject = GetActiveProject();

                        var cmd = sender as OleMenuCommand;
                        if (cmd != null)
                        {
                            cmd.Visible = cmd.Enabled = false;

                            if (IsJspsProject(activeProject.FullName) && ContainsNjsProj(activeProject.FullName))
                            {
                                cmd.Visible = cmd.Enabled = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // send telemetry event
                    }
                });
            }
        }

        private bool ContainsNjsProj(string projectFilepath)
        {
            string projectDir = Path.GetDirectoryName(projectFilepath);
            return Directory.GetFiles(projectDir).Where(file => file.EndsWith(".njsproj")).Any();
        }

        private bool IsJspsProject(string filepath)
        {
            string fileExtension = Path.GetExtension(filepath);
            return (!string.IsNullOrEmpty(fileExtension)) && (fileExtension == ".esproj");
        }

        private EnvDTE.Project GetActiveProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Array activeProjects = (Array)NodejsPackage.Instance.DTE.ActiveSolutionProjects;
            EnvDTE.Project project = (EnvDTE.Project)activeProjects.GetValue(0);

            return project;
        }
    }
}
