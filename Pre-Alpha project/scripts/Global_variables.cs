using Godot;
using System;

public partial class Global_variables : Node
{
    public string ScriptDesc { get { return GetScriptDesc(); } }
    internal string GetScriptDesc() { return "This is a global script that will be autoloaded."; }
    private float saved_FOV = 90;
    public float SavedFOV { get { return saved_FOV; } set { saved_FOV = value; } }
    public string GrappleStr { get { return "grapple"; } }
}