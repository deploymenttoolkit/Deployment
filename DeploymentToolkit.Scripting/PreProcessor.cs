using NLog;
using System.Diagnostics;
using System.Linq;

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

                var isFunction = false;
                if(variableName.Contains("("))
                {
#if DEBUG && PREPROCESSOR_TRACE
                    Debug.WriteLine("Function detected");
#endif
                    isFunction = true;

                    end = part.IndexOf(')');
                    if (end == -1)
                    {
                        end = part.IndexOf(_separator);
                        variableName = part.Substring(0, end);
                        processed = processed.Replace($"${variableName}$", "INCOMPLETE FUNCTION");
                        break;
                    }

                    variableName = part.Substring(0, ++end);

#if DEBUG && PREPROCESSOR_TRACE
                    Debug.WriteLine($"End updated to {end} ({part.Length}, {start})");
#endif
                }

#if DEBUG && PREPROCESSOR_TRACE
                Debug.WriteLine($"Found {variableName}");
#endif
                toProcess = toProcess.Substring(end, toProcess.Length - end);
#if DEBUG && PREPROCESSOR_TRACE
                Debug.WriteLine($"Remaining to Process: {toProcess}");
#endif
                if (isFunction)
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
#if DEBUG && PREPROCESSOR_TRACE
                    Debug.WriteLine($"Function: {functionName}");
                    Debug.WriteLine($"Params: {parameterString}");
#endif
                    if (_functions.ContainsKey(functionName))
                    {
                        // Trim each variable so you can create better visibility in scripts with spaces
                        var parameters = ProcessVariables(parameterString.Split(','));
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
#if DEBUG && PREPROCESSOR_TRACE
            Debug.WriteLine($"Processed: {processed}");
#endif
            return processed;
        }

        private static string[] ProcessVariables(string[] variables)
        {
            var result = new string[variables.Length];
            var trimmed = variables.Select((p) => p.Trim()).ToArray();
            for(var i = 0; i < variables.Length; i++)
            {
                var currentVariable = trimmed[i];
                if(currentVariable.StartsWith("$") && currentVariable.EndsWith("$"))
                {
                    var variableName = currentVariable.Trim('$');
                    if (_variables.ContainsKey(variableName))
                    {
                        result[i] = _variables[variableName].Invoke();
#if DEBUG && PREPROCESSOR_TRACE
                        Debug.WriteLine($"Inline Variable: '{variableName}' -> '{result[i]}'");
#endif  
                    }
                    else
                    {
#if DEBUG && PREPROCESSOR_TRACE
                        Debug.WriteLine($"Invalid inline Variable: '{variableName}'");
#endif
                        result[i] = "VARIABLE NOT FOUND";
                    }
                }
                else
                {
                    result[i] = currentVariable;
                }
            }
            return result;
        }
    }
}
