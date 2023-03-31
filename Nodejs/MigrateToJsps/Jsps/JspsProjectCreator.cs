using System.IO;

namespace MigrateToJsps
{
    internal class JspsProjectCreator
    {
        private string projectDir;
        private NjsprojFileModel njsprojFileModel;

        public JspsProjectCreator(string projectDir, NjsprojFileModel njsprojFileModel)
        {
            this.projectDir = projectDir;
            this.njsprojFileModel = njsprojFileModel;
        }

        public string CreateJspsProject() // to be called by outsiders
        {
            LaunchJsonWriter.CreateLaunchJson(projectDir, njsprojFileModel);

            NugetConfigWriter.GenerateNugetConfig(projectDir);

            var port = njsprojFileModel.NodejsPort;

            return EsprojFileWriter.WriteEsproj(projectDir, njsprojFileModel.ProjectName, njsprojFileModel.StartupFile, port);
        }
    }
}
