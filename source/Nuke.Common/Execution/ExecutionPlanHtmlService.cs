﻿// Copyright 2018 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Common.Execution
{
    internal class ExecutionPlanHtmlService
    {
        private const string c_htmlFileName = "execution-plan.html";

        public static void ShowPlan(IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            var resourceText = ResourceUtility.GetResourceText<ExecutionPlanHtmlService>(c_htmlFileName);
            var contents = resourceText
                .Replace("__GRAPH__", GetGraphDefinition(executableTargets))
                .Replace("__EVENTS__", GetEventsDefinition(executableTargets));
            
            var path = Path.Combine(NukeBuild.TemporaryDirectory, c_htmlFileName);
            File.WriteAllText(path, contents);

            // Workaround for https://github.com/dotnet/corefx/issues/10361
            Process.Start(new ProcessStartInfo
                          {
                              FileName = path,
                              UseShellExecute = true
                          });
        }

        private static string GetGraphDefinition(IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            var builder = new StringBuilder();
            foreach (var executableTarget in executableTargets)
            {
                var dependencies = executableTargets.Where(x => x.AllDependencies.Contains(executableTarget)).ToList();
                if (dependencies.Count == 0)
                    builder.AppendLine(executableTarget.Name);
                else
                {
                    foreach (var dependency in dependencies)
                    {
                        if (dependency.ExecutionDependencies.Contains(executableTarget))
                            builder.AppendLine($"{executableTarget.Name} --> {dependency.Name}");
                        else if (dependency.OrderDependencies.Contains(executableTarget))
                            builder.AppendLine($"{executableTarget.Name} -.-> {dependency.Name}");
                        else if (dependency.TriggerDependencies.Contains(executableTarget))
                            builder.AppendLine($"{executableTarget.Name} ==> {dependency.Name}");
                    }
                }
            }

            return builder.ToString();
        }

        private static string GetEventsDefinition(IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            var builder = new StringBuilder();
            builder.AppendLine($@"  $(""#{executableTargets.Single(x => x.IsDefault).Name}"").find('rect').addClass('defaultTarget');");
            
            foreach (var executableTarget in executableTargets)
            {
                var executionPlan = ExecutionPlanner.GetExecutionPlan(executableTargets, new[] { executableTarget.Name });
                builder
                    .AppendLine($@"  $(""#{executableTarget.Name}"").hover(")
                    .AppendLine("    function() {");
                executionPlan.ForEach(x => builder.AppendLine($@"        $(""#{x.Name}"").addClass('highlight');"));
                builder
                    .AppendLine("    },")
                    .AppendLine("    function() {");
                executionPlan.ForEach(x => builder.AppendLine($@"        $(""#{x.Name}"").removeClass('highlight');"));
                builder
                    .AppendLine("    });");
            }
            

            return builder.ToString();
        }
    }
}