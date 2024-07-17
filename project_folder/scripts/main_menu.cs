using Godot;
using System;

public partial class main_menu : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnButtonsPressed(int n) {
		switch (n) {
			case 0: GD.Print("play"); /*GetTree().ChangeSceneToFile("res://scenes/test_area.tscn");*/ break;
			case 1: GetTree().ChangeSceneToFile("res://scenes/options_menu.tscn"); break;
			case 2: GD.Print("controls"); break;
			case 3: GetTree().Quit(); break;
			default: GD.Print("[color=RED]ERROR[color=WHITE]: main_menu.tscn Button Signal Issue. [color=BLUE]n = " + n); break;
		}
	}
}
