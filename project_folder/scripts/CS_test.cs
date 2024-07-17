using Godot;
using System;

public partial class CS_test : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("A C# script has run successefully.");
		Label label_node = (Label)GetNode("Label"); //This method will return an exception if the node path is incorrect
		label_node.Text = "This text was set by a C#.";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//This is where _Process code would go
	}

	private void OnExitPressed() 
	{
		GetTree().ChangeSceneToFile("res://scenes/debug_menu.tscn");
	}

	private void _on_text_pressed() 
	{
		GD.Print("This signal function name is valid, tho it doesn't follow the typical C# naming scheme.");
		Label label_node = (Label)GetNode("Label"); 
		label_node.Text = "_on_text_pressed() is a valid signal function name";
	}
}
