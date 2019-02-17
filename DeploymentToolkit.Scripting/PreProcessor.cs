using NLog;
using System.Diagnostics;

namespace DeploymentToolkit.Scripting
{
    public static partial class PreProcessor
    {
        private const string _separator = "$";
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static string Process(string data)
        {
            // Nothing to process
            if (!data.Contains(_separator))
                return data;

            var processed = data;
            var toProcess = data;

            do
            {
                var start = toProcess.IndexOf(_separator);
                if (start == -1)
                    break;

                var part = toProcess.Substring(start + 1, toProcess.Length - start - 1);
                var end = part.IndexOf(_separator);
                if (end == -1)
                    break;

                var variableName = part.Substring(0, end);
                Debug.WriteLine($"Found {variableName}");
                Debug.WriteLine($"ToProcess: {toProcess}");
                toProcess = toProcess.Substring(end, toProcess.Length - end);

                if (variableName.Contains("("))
                {
                    var variablesStart = variableName.IndexOf('(');
                    var variablesEnd = variableName.IndexOf(')');

                    if (variablesStart == -1 || variablesEnd == -1)
                    {
                        processed = processed.Replace($"${variableName}$", "INCOMPLETE PARAMETERS");
                        continue;
                    }

                    var parameterString = variableName.Substring(variablesStart, variablesEnd - variablesStart).TrimStart('(').TrimEnd(')');
                    if(string.IsNullOrEmpty(parameterString))
                    {
                        processed = processed.Replace($"${variableName}$", "MISSING PARAMETERS");
                        continue;
                    }

                    var functionName = variableName.Substring(0, variablesStart);
                    Debug.WriteLine($"Function: {functionName}");
                    Debug.WriteLine($"Params: {parameterString}");

                    if (_functions.ContainsKey(functionName))
                    {
                        var parameters = parameterString.Split(',');
                        processed = processed.Replace($"${variableName}$", _functions[functionName].Invoke(parameters));
                    }
                    else
                    {
                        processed = processed.Replace($"${variableName}$", "INVALID FUNCTION");
                    }
                }
                else
                {
                    if (_variables.ContainsKey(variableName))
                        processed = processed.Replace($"${variableName}$", _variables[variableName].Invoke());
                    else
                        processed = processed.Replace($"${variableName}$", "VARIABLE NOT FOUND");
                }
            }
            while (toProcess.Contains(_separator));

            Debug.WriteLine($"Processed: {processed}");

            return processed;
        }
    }
}
