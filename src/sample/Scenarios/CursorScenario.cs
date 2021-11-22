namespace Sample.Scenarios;

[SuppressMessage("Performance", "CA1812")]
sealed class CursorScenario : Scenario
{
    public override Task RunAsync()
    {
        Terminal.OutLine("Available commands:");
        Terminal.OutLine();
        Terminal.OutLine("  visible: Toggle cursor visibility.");
        Terminal.OutLine("  <style>: Set cursor style to the given style.");
        Terminal.OutLine();

        while (true)
        {
            Terminal.Out("Command: ");

            switch (Terminal.ReadLine())
            {
                case "visible":
                    var visible = Terminal.IsCursorVisible = !Terminal.IsCursorVisible;

                    Terminal.OutLine("Cursor is now {0}.", visible ? "visible" : "invisible");
                    break;
                case null:
                    break;
                case var style when Enum.TryParse<TerminalCursorStyle>(style, true, out var s):
                    Terminal.OutLine("Cursor style is now {0}.", Terminal.CursorStyle = s);
                    break;
                case var cmd:
                    Terminal.OutLine("Unknown command '{0}'.", cmd);
                    break;
            }
        }
    }
}
