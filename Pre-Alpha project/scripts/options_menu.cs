using Godot;
using System;

//FIXME - Need to read the Godot docs page on multiple resolutions

public partial class options_menu : Control
{
	private readonly Vector2[] reses = new Vector2[15] {
		new(192,144), new(320,240), new(480,360), new(640,480), new(800,600), new(1024,768), new(1152,864), new(1280,720), new(1360,768), new(1366,768), new(1440,900), new(1600,900), new(1680,1050), new(1920,1080), new(2560,1440)
	};
	public override void _Ready()
	{
		OptionButton res = (OptionButton)GetNode("resolutions");
		Vector2 screen = GetViewport().GetVisibleRect().Size;
		
		GD.Print(screen + "\nresolutions.Length = " + reses.Length);
		for (int i = 0; i < reses.Length; i++) {
			GD.Print("i = " + i + " | res[i] = " + reses[i]);
			res.AddItem(reses[i].X.ToString() + "x" + reses[i].Y.ToString());
			if (reses[i] == screen) { res.Selected = i;	}
		}

		CheckBox fs = (CheckBox)GetNode("window_mode");
		fs.ButtonPressed = GetWindow().Mode == Window.ModeEnum.Fullscreen;
	}
	public override void _Process(double delta)
	{
	}

	//Signal functions
	private void OnWindowModeToggled(bool toggled) {
		if (toggled) { 
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen); 
		} else { 
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed); 
			//Try to set the window size to the resolution the player wanted
		}
	}
	private void OnQuitPressed() { GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn"); }
}
