using Godot;
using System;
using System.Xml;

/*TODO - The paused HUD should have an options menu that affects things in real time in-game that won't require restarting the game scene. FOV slider is done, but will need to keep this in mind for more things to add
*/

public partial class HUD : Control
{
	/*A series of variables are declared that define what things are shown on the 
	*/

	private bool fps; //Decides if the FPS counter should contain text
	private bool paused = false;
	
	//Action definitions
	public Action<int> FOVChanged;
	public Action<string> ArmReady;

	public override void _Ready()
	{
		fps = true; //Edit this when you learn how to import from a global file
		SetPause(paused);

		//int FOV_level = Global.FOV
		HSlider FOVSlider = (HSlider)GetNode("FOV/slider");
		Label FOVLabel = (Label)GetNode("FOV/value");
		Global_variables Global = (Global_variables)GetNode("/root/Global");
		FOVSlider.Value = Global.SavedFOV;
		FOVLabel.Text = Global.SavedFOV.ToString();

		//Set ColorRects to be the size of the screen
		//GD.Print(GetViewport().GetVisibleRect().Size); 
		Vector2 screen = GetViewport().GetVisibleRect().Size;
		ColorRect grey = (ColorRect)GetNode("greyscale");
		ColorRect pause = (ColorRect)GetNode("pause_image");
		grey.Size = screen;
		pause.Size = screen;
	}
	public void ToggleFPS() { fps = !fps; } //Toggles the FPS 
	public void SetFPSBool(bool input) { fps = input; } //Sets the FPS counter to whatever the input is for easier control
	public void SetSlowdownGrey(bool slow_active, double slow_meter, double SLOW_DURATION, bool slow_runout) 
	{
		Sprite2D cross = (Sprite2D)GetNode("BottomLeft/slowdown/cross");
		cross.Visible = slow_runout;

		double meter = slow_meter / SLOW_DURATION;

		ColorRect grey = (ColorRect)GetNode("greyscale");
		grey.Visible = slow_active;
		Sprite2D dark = (Sprite2D)GetNode("BottomLeft/slowdown/dark");

		//The dark foreground for the grapple is controlled by region_rect
		//As the meter value increases, slow down is used, the region increases and the sprite moves down
 
		Sprite2D sprite = (Sprite2D)GetNode("BottomLeft/slowdown");
		Vector2 sprite_size = new Vector2(sprite.Texture.GetWidth(),sprite.Texture.GetHeight()); 
		dark.RegionRect = new Rect2(0, 0, sprite_size.X, (float)(meter * sprite_size.Y));
		dark.Position = new Vector2(dark.Position.X, -(float)((1-meter) * sprite_size.Y / 2));

	}
	public void TogglePause() { paused = !paused; SetPause(paused); }
	private void SetPause(bool vis) 
	{ 
		Label text = (Label)GetNode("pause_text");
		ColorRect image = (ColorRect)GetNode("pause_image");
		Control FOV = (Control)GetNode("FOV");
		
		text.Visible = vis;
		image.Visible = vis;
		FOV.Visible = vis;

		Control quit = (Control)GetNode("quit");
		quit.Visible = vis;
		if (!vis) 
		{
			Button button = (Button)GetNode("quit/button");
			Label label = (Label)GetNode("quit/label");
			Button yes = (Button)GetNode("quit/yes");
			Button no = (Button)GetNode("quit/no");
			button.Visible = true;
			label.Visible = false;
			yes.Visible = false;
			no.Visible = false;
		}
	}
	public void SetGrappleRayFeedback(RayCast3D ray) 
	{
		Label node = (Label)GetNode("grapple_view");
		Area3D obj = ray.GetCollider() as Area3D;
		node.Text = "";
		if (obj == null) 
		{ 
			node.Text += "null"; 
		} 
		else 
		{ 
			node.Text +=  obj.ToString() + "\n" + obj.Name; 
			if (obj.Name.ToString().Substr(0,7) == "grapple") 
			{
				node.Text += "\n" + "This is indeed a grapple point";
				node.Text += "\n" + ray.GetCollisionNormal();
			}
		}
	}
	public void SetGrappleDetect(bool detected) {
		//TODO - It may be a good idea to make the grapple HUD icon glow when a grapple point is detected so that a grapple being present is more noticable.
		//Just invoke this function anyway, and if this idea is rejected then you just delete it later.
	}
	public void SetDebugText(string text) { ((Label)GetNode("debug_text")).Text = text;	}
	public void ClearDebugText() { ((Label)GetNode("debug_text")).Text = ""; }
	public void AddDebugText(string text, bool newline = true) { ((Label)GetNode("debug_text")).Text += (newline ? "\n" : "") + text; }
	public void PlayArmAnim(string name) 
	{
		//TODO - When working on the animation for the gun and gauntlet, will need to edit this.
		/*Here is a list of animations planned
		~gain_grapple
		~gain_gun
		~change_grapple
		~change_gun
		~use_grapple
		~use_gun
		~reload_gun (which may be more dynamic than one animation, so change later)
		~failed_grapple

		//There may be other animations needed, so this list is incomplete
		There will probably be idle animations for the arms, which will be activated when the animations of the guns end. The idle animations will loop, but the way the signal function would work, by seeing if an animation that has ended was a gun or grapple animation and then playing the right idle animation, it may not be needed to loop the idle animation (especially if the end of a looping animation triggers the "animation_stopped" signal)  
		*/
		AnimationPlayer anim = (AnimationPlayer)GetNode("BottomRight/arm/ArmAnimations");
		anim.Play(name);
	}
	public override void _Process(double delta)
	{
		if (fps) 
		{ 
			Label fps_node = (Label)GetNode("FPS"); 
			fps_node.Text = "FPS:   " + Math.Round(1/delta) + "\nDelta: " + Math.Round(delta, 4); 
		}
	}
	
	//Signals code
	private void OnFOVChanged(float value) 
	{
		Label val_text = (Label)GetNode("FOV/value");
		val_text.Text = ((int)value).ToString();
		FOVChanged?.Invoke((int)value); //The ? is needed because otherwise you get a System.NullReferenceException error
	} 

	private void OnExitButtonsPressed(int n) 
	{		
		//0 is the basic quit, 1 is yes, 2 is no
		//0 brings up the other buttons, 1 quits to menu, 2 brings back normal quit
		switch (n) 
		{
			case 0: 
			case 2:
				
				Button button = (Button)GetNode("quit/button");
				Label label = (Label)GetNode("quit/label");
				Button yes = (Button)GetNode("quit/yes");
				Button no = (Button)GetNode("quit/no");
				button.Visible = n == 2;
				label.Visible = n == 0;
				yes.Visible = n == 0;
				no.Visible = n == 0;
				break;

			case 1: GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn"); break;
			default: GD.PrintRich("[color=RED]ERROR[color=WHITE]: quit signal error"); break;
		}
	}
}
