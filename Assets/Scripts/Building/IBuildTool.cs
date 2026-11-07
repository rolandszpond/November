using System.Collections.Generic;

// A state of the build-tool FSM, plus what the toolbar needs to show for it.
public interface IBuildTool : IState
{
    string Name { get; }
    string Hint { get; }

    // Catalog items this tool can place (empty for tools like Demolish), and which one is selected.
    IReadOnlyList<BuildCatalog.Item> Items { get; }
    int SelectedIndex { get; set; }
}
