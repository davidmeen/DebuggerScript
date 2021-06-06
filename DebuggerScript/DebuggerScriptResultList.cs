using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;

namespace DebuggerScript
{
    public class DebuggerScriptResultList
    {
        public DebuggerScriptResultList()
        {
            Results = new List<DebuggerScriptResult>();
        }

        public void Add(string name, string expression, int index)
        {
            DebuggerScriptResult result = new DebuggerScriptResult(name, expression, index);
            Results.Add(result);
        }

        public void AddLiteral(string name, string expression)
        {
            DebuggerScriptResult result = new DebuggerScriptResult(name, expression, 0);
            result.IsLiteral = true;
            Results.Add(result);
        }

        public DebuggerScriptResultList Array(int count)
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                for (int i = 0; i < count; ++i)
                {
                    newResults.Add("(" + result.Name + ")[" + i + "]", "(" + result.Expression + ")[" + i + "]", i);
                }
            }

            return newResults;
        }

        public DebuggerScriptResultList ArrayRange(int firstIndex, int count)
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                for (int i = 0; i < count; ++i)
                {
                    newResults.Add("(" + result.Name + ")[" + (firstIndex + i) + "]", "(" + result.Expression + ")[" + (firstIndex + i) + "]", firstIndex + i);
                }
            }

            return newResults;
        }

        public DebuggerScriptResultList Members(params string[] memberNames)
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                foreach (string member in memberNames) 
                {
                    newResults.Add("(" + result.Name + ")" + member, "(" + result.Expression + ")" + member, result.Index);
                }
            }
            return newResults;
        }

        public DebuggerScriptResultList ArrayIndex(int index)
        {
            return ArrayRange(index, 1);
        }

        public DebuggerScriptResultList Rename(string name)
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                newResults.Add(name, result.Expression, result.Index);
            }

            return newResults;
        }

        public DebuggerScriptResultList RenameWithIndex(string name)
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                newResults.Add(string.Format("{0}[{1}]", name, result.Index), result.Expression, result.Index);
            }

            return newResults;
        }

        public DebuggerScriptResultList Cast(string newType)
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                newResults.Add(CastString(newType, result.Name), CastString(newType, result.Expression), result.Index);
            }

            return newResults;
        }

        private string CastString(string type, string name)
        {
            return "(" + type +")(" + name +")";
        }

        public DebuggerScriptResultList ReinterpretCast(string newType)
        {
            return Reference().Cast(newType + "*").Pointer();
        }

        public DebuggerScriptResultList Reference()
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                newResults.Add(ReferenceString(result.Name), ReferenceString(result.Expression), result.Index);
            }

            return newResults;
        }

        private string ReferenceString(string name)
        {
            return "&(" + name + ")";
        }

        public DebuggerScriptResultList Pointer()
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                newResults.Add(PointerString(result.Name), PointerString(result.Expression), result.Index);
            }

            return newResults;
        }

        private string PointerString(string name)
        {
            return "*(" + name + ")";
        }

        public List<DebuggerScriptResult> GetResults()
        {
            return Results;
        }

        public DebuggerScriptResultList Memory(string type, int offset, int count)
        {
            DebuggerScriptResultList newResults = Reference().Cast(type + "*").ArrayRange(offset, count);

            for (int i = 0; i < newResults.Results.Count; ++i)
            {
                newResults.Results[i].UseAddressForName = true;

                if (i + offset == 0)
                {
                    newResults.Results[i].Highlight = true;
                }
            }
            return newResults;
        }

        public DebuggerScriptResultList Memory(string type, int count)
        {
            return Memory(type, 0, count);
        }

        public DebuggerScriptResultList Memory(int count)
        {
            return Memory("void*", 0, count);
        }

        public DebuggerScriptResultList Memory(int offset, int count)
        {
            return Memory("void*", offset, count);
        }

        public DebuggerScriptResultList Filter(string filter, EnvDTE.Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                var expr = debugger.GetExpression(string.Format(filter, "(" + result.Expression + ")"), false);
                if (expr != null && expr.Value == "true")
                {
                    newResults.Add(result.Name, result.Expression, result.Index);
                }
            }
            return newResults;
        }
        
        public DebuggerScriptResultList FilterString(string filter, string ExpectedValue, EnvDTE.Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                var expr = debugger.GetExpression(string.Format(filter, "(" + result.Expression + ")"), true);
                if (expr != null && ExtractString(expr.Value) == ExpectedValue)
                {
                    newResults.Add(result.Name, result.Expression, result.Index);
                }
            }
            return newResults;
        }

        public DebuggerScriptResultList FilterNotString(string filter, string ExpectedValue, EnvDTE.Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DebuggerScriptResultList newResults = new DebuggerScriptResultList();
            foreach (var result in Results)
            {
                var expr = debugger.GetExpression(string.Format(filter, "(" + result.Expression + ")"), true);
                if (expr != null && ExtractString(expr.Value) != ExpectedValue)
                {
                    newResults.Add(result.Name, result.Expression, result.Index);
                }
            }
            return newResults;
        }


        public DebuggerScriptResultList Fold(string op)
        {
            DebuggerScriptResultList newResults = new DebuggerScriptResultList();

            if (Results.Count >= 1)
            {
                string name = Results[0].Name;
                string expr = Results[0].Expression;
                for (int resultIndex = 1; resultIndex < Results.Count; ++resultIndex)
                {
                    name = string.Format(op, name, Results[resultIndex].Name);
                    expr = string.Format(op, expr, Results[resultIndex].Expression);
                }
                newResults.Add(name, expr, 0);
            }

            return newResults;
        }


        string ExtractString(string input)
        {
            List<int> quotes = new List<int>();
            for (int i = 0; i < input.Length; ++i)
            {
                if (input[i] == '"')
                {
                    quotes.Add(i);
                }
            }

            if (quotes.Count >= 2)
            {
                return input.Substring(quotes[0] + 1, quotes[1] - quotes[0] - 1);
            }
            else
            {
                return null;
            }
        }

        private List<DebuggerScriptResult> Results;
    }
}
