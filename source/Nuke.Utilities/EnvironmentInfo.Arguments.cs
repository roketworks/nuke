// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Nuke.Common.Utilities;

namespace Nuke.Common
{
    public static partial class EnvironmentInfo
    {
        internal static ArgumentHelper ArgumentHelper = new ArgumentHelper(Environment.GetCommandLineArgs());

        public static IReadOnlyCollection<string> CommandLineArguments => ArgumentHelper.Arguments;

        public static IReadOnlyCollection<string> ParseArguments(string commandLine)
        {
            var inSingleQuotes = false;
            var inDoubleQuotes = false;
            var escaped = false;
            return commandLine.Split((c, _) =>
                    {
                        if (c == '\"' && !inSingleQuotes && !escaped)
                            inDoubleQuotes = !inDoubleQuotes;

                        if (c == '\'' && !inDoubleQuotes && !escaped)
                            inSingleQuotes = !inSingleQuotes;

                        escaped = c == '\\' && !escaped;

                        return c == ' ' && !(inDoubleQuotes || inSingleQuotes);
                    },
                    includeSplitCharacter: true)
                .Select(x => x.Trim().TrimMatchingDoubleQuotes().TrimMatchingQuotes().Replace("\\\"", "\"").Replace("\\\'", "'"))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
        }

        public static bool HasArgument(string name)
        {
            return ArgumentHelper.HasArgument(name);
        }

        public static bool HasArgument<T>(Expression<Func<T>> expression)
        {
            return HasArgument(expression.GetMemberInfo().Name);
        }

        [CanBeNull]
        public static T GetNamedArgument<T>(string parameterName, char? separator = null)
        {
            return (T) ArgumentHelper.GetNamedArgument(parameterName, typeof(T), separator);
        }

        [CanBeNull]
        public static T GetNamedArgument<T>(Expression<Func<T>> expression, char? separator = null)
        {
            return GetNamedArgument<T>(expression.GetMemberInfo().Name, separator);
        }

        [CanBeNull]
        public static T GetNamedArgument<T>(Expression<Func<object>> expression, char? separator = null)
        {
            return GetNamedArgument<T>(expression.GetMemberInfo().Name, separator);
        }

        [CanBeNull]
        public static T GetPositionalArgument<T>(int position, char? separator = null)
        {
            return (T) ArgumentHelper.GetPositionalArgument(position, typeof(T), separator);
        }

        [CanBeNull]
        public static T[] GetAllPositionalArguments<T>(char? separator = null)
        {
            return (T[]) ArgumentHelper.GetAllPositionalArguments(typeof(T), separator);
        }
    }
}
