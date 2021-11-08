// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using Nuke.Common.ValueInjection;
using Nuke.Common.Utilities;
using Serilog;

namespace Nuke.Common.Execution
{
    [PublicAPI]
    public class HandleVisualStudioDebuggingAttribute : BuildExtensionAttributeBase, IOnBuildCreated
    {
        private const string TemporaryArgumentFileName = "nuke.tmp";

        public int TimeoutInMilliseconds { get; } = 10_000;

        public void OnBuildCreated(
            NukeBuild build,
            IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            if (!ParameterService.GetParameter<bool>(Constants.VisualStudioDebugParameterName))
                return;

            File.WriteAllText(Constants.GetVisualStudioDebugFile(NukeBuild.RootDirectory),
                Process.GetCurrentProcess().Id.ToString());
            Assert.True(SpinWait.SpinUntil(() => Debugger.IsAttached, millisecondsTimeout: TimeoutInMilliseconds),
                $"VisualStudio debugger was not attached within {TimeoutInMilliseconds} milliseconds");

            EnvironmentInfo.CommandLineArguments = GetSurrogateArguments();
        }

        [CanBeNull]
        private static string[] GetSurrogateArguments()
        {
            var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
            if (entryAssemblyLocation.IsNullOrEmpty())
                return null;

            var assemblyDirectory = Path.GetDirectoryName(entryAssemblyLocation).NotNull();
            var argumentsFile = Path.Combine(assemblyDirectory, TemporaryArgumentFileName);
            if (!File.Exists(argumentsFile))
                return null;

            var argumentLines = File.ReadAllLines(argumentsFile);
            var lastWriteTime = File.GetLastWriteTime(argumentsFile);

            Assert.HasSingleItem(argumentLines, $"{TemporaryArgumentFileName} must have only one single line");
            File.Delete(argumentsFile);
            // TODO: use timeout
            if (lastWriteTime.AddMinutes(value: 1) < DateTime.Now)
            {
                Log.Warning("Last write time of {File} was {LastWriteTime}. Skipping ...", TemporaryArgumentFileName, lastWriteTime);
                return null;
            }

            var splittedArguments = EnvironmentInfo.ParseArguments(argumentLines.Single());
            return new[] { entryAssemblyLocation }.Concat(splittedArguments).ToArray();
        }
    }
}
