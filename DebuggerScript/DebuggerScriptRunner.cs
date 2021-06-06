using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DebuggerScript
{
    public class DebuggerScriptRunner
    {
        public DebuggerScriptRunner()
        {
            SavedResults = new Dictionary<string, DebuggerScriptResultList>();
        }

        public virtual DebuggerScriptResultList Execute(string script, EnvDTE.Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            DebuggerScriptResultList results = null;

            foreach (string line in script.Split('\r', '\n'))
            {
                if (!String.IsNullOrEmpty(line) && !line.StartsWith("//"))
                {
                    results = ExecuteLine(line, debugger);
                }
            }

            return results;
        }

        public DebuggerScriptResultList ExecuteLine(string script, EnvDTE.Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string token;
            List<string> args;
            bool isFunction;
            string saveTo = null;

            DebuggerScriptResultList results = new DebuggerScriptResultList();

            string functionDeclStart = "function ";
            if (script.ToLower().StartsWith(functionDeclStart))
            {
                string functionDecl = script.Substring(functionDeclStart.Length);
                string restOfScript; 
                GetNextToken(functionDecl, out FunctionBeingAdded.Name, out FunctionBeingAdded.Args, out isFunction, out restOfScript);
                if (!FunctionBeingAdded.Name.StartsWith("#"))
                {
                    throw new Exception("Function names must start with #");
                }
                FunctionBeingAdded.Script = "";
                IsAddingFunction = true;
                return null;
            }
            else if (IsAddingFunction)
            {
                if (script.ToLower() == "end")
                {
                    IsAddingFunction = false;
                    Functions.Add(FunctionBeingAdded);
                }
                else
                {
                    FunctionBeingAdded.Script = FunctionBeingAdded.Script + script + "\n";
                }
                return null;
            }

            GetNextToken(script, out token, out args, out isFunction, out script);
            
            do
            {
                if (isFunction)
                {
                    switch (token.ToLower())
                    {
                        case ".array":
                            results = results.Array(int.Parse(GetArgValue(args[0], debugger)));
                            break;
                        case ".arrayrange":
                            results = results.ArrayRange(int.Parse(GetArgValue(args[0], debugger)), int.Parse(GetArgValue(args[1], debugger)));
                            break;
                        case ".cast":
                            results = results.Cast(args[0]);
                            break;
                        case "concat":
                        case ".concat":
                            results = Concat(args.ToArray());
                            break;
                        case ".filter":
                            if (args.Count != 1) throw new Exception("filter requires 1 argument");
                            results = results.Filter(GetArgValue(args[0], debugger), debugger);
                            break;
                        case ".filterstring":
                            if (args.Count != 2) throw new Exception("filterstring requires 2 arguments");
                            results = results.FilterString(GetArgValue(args[0], debugger), GetArgValue(args[1], debugger), debugger);
                            break;
                        case ".filternotstring":
                            if (args.Count != 2) throw new Exception("filternotstring requires 2 arguments");
                            results = results.FilterNotString(GetArgValue(args[0], debugger), GetArgValue(args[1], debugger), debugger);
                            break;
                        case ".fold":
                            if (args.Count != 1) throw new Exception("fold requires 1 argument");
                            results = results.Fold(args[0]);
                            break;
                        case ".index":
                            results = results.ArrayIndex(int.Parse(GetArgValue(args[0], debugger)));
                            break;
                        case ".members":
                            results = results.Members(args.ToArray());
                            break;
                        case ".memory":
                            results = Memory(args, results);
                            break;
                        case ".pointer":
                            results = results.Pointer();
                            break;
                        case ".reference":
                            results = results.Reference();
                            break;
                        case ".reinterpretcast":
                            results = results.ReinterpretCast(GetArgValue(args[0], debugger));
                            break;
                        case ".rename":
                            results = results.Rename(GetArgValue(args[0], debugger));
                            break;
                        case ".renamewithindex":
                            results = results.RenameWithIndex(GetArgValue(args[0], debugger));
                            break;
                        case "zip":
                        case ".zip":
                            results = Zip(args.ToArray());
                            break;
                        case "zipwith":
                        case ".zipwith":
                            results = ZipWith(args.ToArray());
                            break;
                        case "import":
                            Import(args[0], debugger);
                            break;
                        case "=":
                            saveTo = args[0];
                            if (!saveTo.StartsWith("$"))
                            {
                                throw new Exception("Variables must begin with '$'");
                            }
                            break;
                        default:
                            if (token.StartsWith("#"))
                            {
                                results = RunFunction(token, results, args, debugger);
                            }
                            else if (token.StartsWith(".#"))
                            {
                                results = RunFunction(token.Substring(1), results, args, debugger);
                            }
                            else
                            {
                                throw new Exception("Unknown function");
                            }
                            break;
                    }
                }
                else if (token.StartsWith(".") || token.StartsWith("->"))
                {
                    results = results.Members(token);
                }
                else if (token.StartsWith("$"))
                {
                    if (!SavedResults.ContainsKey(token))
                    {
                        throw new Exception(String.Format("Using unknown variable {0}", token));
                    }
                    results = SavedResults[token];
                }                
                else
                {
                    results = GetVariable(token);
                }                
            } while (GetNextToken(script, out token, out args, out isFunction, out script));

            if (saveTo != null)
            {
                SavedResults.Add(saveTo, results);
            }
            return results;
        }

        DebuggerScriptResultList RunFunction(string name, DebuggerScriptResultList input, List<string> args, EnvDTE.Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (Function function in Functions)
            {
                if (function.Name == name && function.Args.Count == args.Count)
                {
                    DebuggerScriptRunner functionRunner = new DebuggerScriptRunner();
                    functionRunner.AddFunctions(Functions);
                    functionRunner.AddResult("$this", input);
                    for (int i = 0; i < args.Count; ++i)
                    {
                        if (args[i].StartsWith("$"))
                        {
                            functionRunner.AddResult(function.Args[i], SavedResults[args[i]]);
                        }
                        else
                        {
                            DebuggerScriptResultList argResult = new DebuggerScriptResultList();
                            argResult.AddLiteral(args[i], args[i]);
                            functionRunner.AddResult(function.Args[i], argResult);
                        }
                    }
                    return functionRunner.Execute(function.Script, debugger);
                }
            }
            throw new Exception("No matching function: " + name);
        }

        private static DebuggerScriptResultList Memory(List<string> args, DebuggerScriptResultList results)
        {
            int arg0Value;
            bool hasType = !int.TryParse(args[0], out arg0Value);

            switch (args.Count)
            {
                case 1:
                    results = results.Memory(arg0Value);
                    break;
                case 2:
                    if (hasType)
                    {
                        results = results.Memory(args[0], int.Parse(args[1]));
                    }
                    else
                    {
                        results = results.Memory(arg0Value, int.Parse(args[1]));
                    }
                    break;
                case 3:
                    results = results.Memory(args[0], int.Parse(args[1]), int.Parse(args[2]));
                    break;
            }

            return results;
        }

        string GetArgValue(string arg, EnvDTE.Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (arg.StartsWith("$"))
            {
                if (!SavedResults.ContainsKey(arg))
                {
                    throw new Exception(String.Format("Using unknown variable {0}", arg));
                }
                var indexResult = SavedResults[arg].GetResults()[0];
                if (indexResult.IsLiteral)
                {
                    return indexResult.Expression;
                }
                else
                {
                    var expr = debugger.GetExpression(indexResult.Expression + ",d");
                    return expr.Value;
                }
            }
            else
            {
                return arg;
            }
        }

        DebuggerScriptResultList Concat(string[] args)
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            
            for (int inputIndex = 0; inputIndex < args.Count(); ++inputIndex)
            {
                if (!SavedResults.ContainsKey(args[inputIndex]))
                {
                    throw new Exception(String.Format("Using unknown variable {0}", args[inputIndex]));
                }
                var inputResults = SavedResults[args[inputIndex]];

                for (int resultIndex = 0; resultIndex < inputResults.GetResults().Count(); ++resultIndex)
                {
                    string exprString = inputResults.GetResults()[resultIndex].Expression;
                    string nameString = inputResults.GetResults()[resultIndex].Name;
                    newResults.Add(exprString, nameString, inputResults.GetResults()[resultIndex].Index);
                }
            }

            return newResults;
        }


        DebuggerScriptResultList Zip(string[] args)
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            DebuggerScriptResultList[] inputResults;
            int minResults;
            GetInputResultsForZip(args, 0, out inputResults, out minResults);

            for (int resultIndex = 0; resultIndex < minResults; ++resultIndex)
            {
                for (int inputIndex = 0; inputIndex < inputResults.Count(); ++inputIndex)
                {
                    string exprString = inputResults[inputIndex].GetResults()[resultIndex].Expression;
                    string nameString = inputResults[inputIndex].GetResults()[resultIndex].Name;
                    newResults.Add(exprString, nameString, inputResults[inputIndex].GetResults()[resultIndex].Index);
                }
            }

            return newResults;
        }

        DebuggerScriptResultList ZipWith(string[] args)
        {
            string func = args[0];
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            DebuggerScriptResultList[] inputResults;
            int minResults;
            GetInputResultsForZip(args, 1, out inputResults, out minResults);

            for (int resultIndex = 0; resultIndex < minResults; ++resultIndex)
            {
                string[] formatArgs = new string[inputResults.Count()];
                for (int inputIndex = 0; inputIndex < inputResults.Count(); ++inputIndex)
                {
                    formatArgs[inputIndex] = "(" + inputResults[inputIndex].GetResults()[resultIndex].Expression + ")";
                }
                int sourceIndex = 0;
                if (inputResults.Count() > 0)
                {
                    sourceIndex = inputResults[0].GetResults()[resultIndex].Index;
                }
                string exprString = string.Format(func, formatArgs);
                newResults.Add(exprString, exprString, sourceIndex);
            }

            return newResults;
        }

        private void GetInputResultsForZip(string[] args, int offset, out DebuggerScriptResultList[] inputResults, out int minResults)
        {
            minResults = 0;
            inputResults = new DebuggerScriptResultList[args.Count() - offset];

            for (int inputIndex = 0; inputIndex < inputResults.Count(); ++inputIndex)
            {
                if (!SavedResults.ContainsKey(args[inputIndex + offset]))
                {
                    throw new Exception(String.Format("Using unknown variable {0}", args[inputIndex + offset]));
                }
                inputResults[inputIndex] = SavedResults[args[inputIndex + offset]];

                int count = inputResults[inputIndex].GetResults().Count();
                minResults = inputIndex == 0 ? count : Math.Min(count, minResults);
            }
        }

        // TODO: Clean up, make faster!
        bool GetNextToken(string script, out string token, out List<string> args, out bool isFunction, out string rest)
        {
            args = new List<string>();
            token = script;
            rest = "";
            isFunction = false;

            if (String.IsNullOrEmpty(script))
            {
                return false;
            }

            if (script.Substring(0, 1) == "[")
            {
                // Doesn't support complex array indices.
                for (int CharIndex = 1; CharIndex < script.Length; ++CharIndex)
                {
                    if (script[CharIndex] == ']')
                    {
                        token = ".Index";
                        args.Add(script.Substring(1, CharIndex - 1));
                        isFunction = true;
                        rest = script.Substring(CharIndex + 1);
                        return true;
                    }
                }
                throw new Exception("Missing '['");
            }

            for (int CharIndex = 1; CharIndex < script.Length; ++CharIndex)
            {
                if (script.Substring(CharIndex, 1) == "." ||
                    (script.Length > CharIndex + 1 && script.Substring(CharIndex, 2) == "->") ||
                    script.Substring(CharIndex, 1) == "(" ||
                    script.Substring(CharIndex, 1) == "[" ||
                    script.Substring(CharIndex, 1) == "=")
                {
                    token = script.Substring(0, CharIndex);

                    if (script[CharIndex] == '=')
                    {
                        // Change into a function with the name as an argument;
                        rest = script.Substring(CharIndex + 1);
                        args.Add(token);
                        token = "=";
                        isFunction = true;
                    }
                    else if (script[CharIndex] == '(')
                    {
                        int paranCount = 1;
                        int argStart = CharIndex + 1;
                        isFunction = true;

                        for (int InnerIndex = CharIndex + 1; InnerIndex < script.Length; ++InnerIndex)
                        {
                            if (script.Substring(InnerIndex, 1) == "," && paranCount == 1)
                            {
                                args.Add(script.Substring(argStart, InnerIndex - argStart));
                                argStart = InnerIndex + 1;
                            }
                            else if (script.Substring(InnerIndex, 1) == "(")
                            {                                
                                ++paranCount;
                            }
                            else if (script.Substring(InnerIndex, 1) == ")")
                            {
                                --paranCount;

                                if (paranCount == 0)
                                {
                                    string arg = script.Substring(argStart, InnerIndex - argStart).Trim();
                                    if (!String.IsNullOrEmpty(arg))
                                    {
                                        args.Add(arg);
                                    }
                                    rest = script.Substring(InnerIndex + 1);
                                    return true;
                                }
                            }
                        }

                        throw new Exception("Missing ')'");
                    }
                    else
                    {
                        rest = script.Substring(CharIndex);
                    }
                    return true;
                }                
            }
            return true;
        }

        void Import(string file, EnvDTE.Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string fullPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\DebuggerScript\\" + file;
            using (StreamReader stream = File.OpenText(fullPath))
            {
                while (!stream.EndOfStream)
                {
                    string line = stream.ReadLine();
                    ExecuteLine(line, debugger);
                }
            }
        }

        public DebuggerScriptResultList GetVariable(string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DebuggerScriptResultList results = new DebuggerScriptResultList();
            results.Add(name, name, 0);

            return results;
        }

        public void AddResult(string name, DebuggerScriptResultList value)
        {
            SavedResults.Add(name, value);
        }

        public void AddFunctions(List<Function> functionsToAdd)
        {
            Functions.AddRange(functionsToAdd);
        }

        Dictionary<string, DebuggerScriptResultList> SavedResults;

        public struct Function
        {
            public string Name;
            public List<string> Args;
            public string Script;
        }

        List<Function> Functions = new List<Function>();
        Function FunctionBeingAdded;
        bool IsAddingFunction = false;
    }
}
