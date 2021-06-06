using System;
using Microsoft.VisualStudio.Shell;

namespace DebuggerScript
{
    public class DebuggerScriptResult
    {
        public DebuggerScriptResult(string name, string expression)
        {
            Name = name;
            Expression = expression;
            Value = "<Not evaluated>";
            Type = "";
        }

        public DebuggerScriptResult(string error)
        {
            Name = "Error";
            Expression = "";
            Value = error;
            Type = "";
        }

        public void Evaluate(EnvDTE.Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Value = "<Unknown value>";

            if (IsLiteral)
            {
                Value = Expression;
                return;
            }

            try
            {
                if (debugger != null)
                {
                    EnvDTE.Expression expr = debugger.GetExpression(Expression, true);
                    if (expr != null)
                    {
                        Value = expr.Value;
                        Type = expr.Type;
                    }

                    if (UseAddressForName)
                    {
                        EnvDTE.Expression nameExpr = debugger.GetExpression("&" + Expression, false);
                        if (nameExpr != null)
                        {
                            Name = nameExpr.Value;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Value = "Exception thrown: " + e.Message;
            }            
        }

        public string Name { get; private set; }
        public string Value
        {
            get; private set;
        }
        public string Type
        {
            get; private set;
        }
        public string Expression { get; private set; }
        public bool UseAddressForName { get; set; }
        public bool Highlight { get; set; }
        public bool IsLiteral { get; set; }
    }
}
