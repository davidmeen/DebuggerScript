namespace DebuggerScript
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using System.CodeDom.Compiler;

    /// <summary>
    /// Interaction logic for DebuggerScriptToolWindowControl.
    /// </summary>
    public partial class DebuggerScriptToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DebuggerScriptToolWindowControl"/> class.
        /// </summary>
        public DebuggerScriptToolWindowControl()
        {
            this.InitializeComponent();
        }

        public class DGItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
        };

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.Items.Clear();

            DebuggerScriptRunner command = new DebuggerScriptRunner();
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                DTE dte = (DTE)Package.GetGlobalService(typeof(DTE));

                var results = command.Execute(scriptBox.Text, dte.Debugger);

                foreach (var result in results.GetResults())
                {
                    result.Evaluate(dte.Debugger);
                    dataGrid.Items.Add(result);
                }
            }
            catch (System.Exception ex)
            {
                DebuggerScriptResult result = new DebuggerScriptResult(ex.Message);
                dataGrid.Items.Add(result);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MyToolWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Point offset = dataGrid.TransformToAncestor(MyToolWindow).Transform(new Point(0, 0));
            dataGrid.Height = e.NewSize.Height - offset.Y;
        }
    }
}