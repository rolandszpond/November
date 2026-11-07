using System;
using System.Collections.Generic;

// No tool selected: build mode is open but clicks do nothing.
public class NoTool : IBuildTool
{
    public string Name => "None";
    public string Hint => "1 Wall   2 Floor   3 Object   4 Demolish      WASD pan   Q/E rotate   Wheel zoom   B exit";
    public IReadOnlyList<BuildCatalog.Item> Items => Array.Empty<BuildCatalog.Item>();
    public int SelectedIndex { get; set; }

    public void Enter() { }
    public void Update() { }
    public void Exit() { }
}
