using System;
using System.Collections.Generic;
using Speedway.Core;
using Speedway.Core.Resources;

namespace Speedway.PipelineBuilderApi.Ports.Adapters
{
    public static class InitialContent
    {
        public static SpeedwayManifest SpeedwayManifest(Guid projectId, string slug, string displayName,
            string currentUser)
        {
            return new SpeedwayManifest(
                projectId,
                "1.0",
                slug,
                displayName,
                new List<SpeedwayResourceMetadata>()
                {
                    new SpeedwaySecretContainerResourceMetadata("Default", null)
                },
                new[] {currentUser},
                Array.Empty<string>());
        }

        public static string SpeedwayDevopsPipeline(
            Guid projectId,
            string storageAccountName,
            string deploymentApiUri,
            string deploymentApiApplicationId)
        {
            return $@"
pool:
  vmImage: 'ubuntu-18.04' 

resources:
  repositories:
    - repository: templates
      type: git
      name: Speedway/Speedway

extends:
  template: master-template.yml@templates # Template reference
  parameters:
    projectId: {projectId}
    storageAccountName: {storageAccountName}
    deploymentApiUri: {deploymentApiUri}
    deploymentApiApplicationId: {deploymentApiApplicationId}
";
        }

        /// <summary>
        /// Simple build file to get it all going
        /// </summary>
        /// <returns></returns>
        public static string BuildSh()
        {
            return @"
#!/bin/bash

for csproj in ./test/**/*.csproj; do
    dotnet test --collect:""XPlat Code Coverage"" --results-directory $COMMON_TESTRESULTSDIRECTORY --logger:trx $csproj
done
sp
for csproj in ./src/**/*.csproj; do
    csprojFile=${csproj##*/}
    csprojName=${csprojFile%.*}
    dotnet publish $csproj -p:Version=$BUILD_SEMVER -o $BUILD_ARTIFACTSTAGINGDIRECTORY/artifacts/$csprojName/
done

echo ""Building to $BUILD_BINARIESDIRECTORY using version $BUILD_SEMVER""
                ".Replace(Environment.NewLine, "\n");
        }

        /// <summary>
        /// Starting with a nice .net core ignore file
        /// https://github.com/dotnet/core/blob/master/.gitignore
        /// </summary>
        /// <returns></returns>
        public static string GitIgnore()
        {
            return @"
*.swp
*.*~
project.lock.json
.DS_Store
*.pyc
nupkg/

# Visual Studio Code
.vscode

# Rider
.idea

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
build/
bld/
[Bb]in/
[Oo]bj/
[Oo]ut/
msbuild.log
msbuild.err
msbuild.wrn

# Visual Studio 2015
.vs/";
        }

        public static string GitVersionConfig = @"mode: Mainline";
    }
}