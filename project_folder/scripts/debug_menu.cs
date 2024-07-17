using Godot;
using System;
using System.Collections.Generic;

public partial class debug_menu : Node2D
{
	// Called when the node enters the scene tree for the first time.
	private string[] scene_names = {"CS_test","pronouns_test"};

	private void _on_button_pressed(String name) 
	{
		bool found = false;
		foreach(string i in scene_names) { if (i == name) { found = true; break; } }

		if (found) {
			Error result = GetTree().ChangeSceneToFile("res://scenes/" + name + ".tscn");
			if (result is Error.Ok) return; else { GD.Print("Couldn't load " + name + ".tscn"); }
		} else {
			GD.Print("Have not got an array element for the string '" + name + "'");
		}
	}
}
