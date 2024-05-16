using Godot;
using System;
using System.Collections.Generic;

/*Todo section
TODO - Runnable walls have been replaced by walls that you can jump off, and grind rails. The grind rails still appear to be a bit tough to create, but it's still easier than designing runnable walls.

TODO - Need a better control scheme. The issue is that you can't map shoot, grapple, and slowdown to the mouse. Other solutions are:
  ~~Shoot and grapple should be on left and right mouse respectively, but doing that means that I can't have an alt-fire for the gun unless the alt-fire is attached to holding the button.
  ~~I could make the grapple and the gun seperate abilities which can be cycled between, but that means that challenges that require shooting things and grappling are harder to play (if done right, it could be like Onnamusha, the Furi character)
  ~~

TODO - Implement the outlines of the grapple icon in the HUD, and the outline sprite of the grapple point being pointed at. The basic idea is to save the node that was looked at on the previous physics step (if there was none, it will equal null because of the "as Area3D" part). Then, when the player stops looking at the grapple point ('current grapple != previous grapple' will catch null or a different grapple point), the code will set the outline sprite of that point to be invisible. This also can be simplified because if the player is looking at a grapple point on that frame, then the outline sprite can be set to invisible. (WILL probably need to filter out other Area3D nodes that aren't grapples btw)

TODO - Have a think about how the ability recharges would go and how they work with the "big punishment angle". Will need to discuss this in a larger document.

TODO - Really need to get around to properly implementing the grapple icon in the bottom of the screen, as well as the cooldown
This means adding the cooldown code (just copy the slow code) but it also means having it tell you when a grapple point is within reach.

TODO - Need to start thinking about the gun. Let's break that down
  ~~Implementing the gun's functionality: ammo count, reload animation that's smaller depending on the amount of ammo used, and HUD mmo count (might be a good idea to have it be a segmented block like the Crash Bandicoot health bars)
  ~~The visual design and structure of the model
  ~~Thinking about the animations (it needs the battery that pings out, and I need to think about the battery colour animation and how that would work with potentially having different length) 
  ~~Think of how it would work with the platforming challenges. Obviously include breakable walls that would regenerate, but also it would be fun to be able to use the gun as a possible double jump (the movement code could be a nightmare tho) 

TODO - Would be better to have some simple but obvious icons for the abilities (slowdown, grapple, dodge, gun) so that I can track what means what and simply swap out the shitty textures with better textures when the game is nearing completion.

TODO - Need to revise the jump buffer code and implement coyote time. (https://www.youtube.com/watch?v=RFix_Kg2Di0)
*/

public partial class Player : CharacterBody3D
{
	//To check the number of variants of an enum, use Enum.GetNames(typeof(TheEnum)).Length;
	const double SPEED = 15.0; 
	const double ACCEL = 10.0; 
	const double GRAVITY = 30.0; 
	const double TERMINAL_VELOCITY = -120.0;
	private Vector3 move_dir = Vector3.Zero; //Move direction (derived from inputs)
	const double MOUSE_SEN = 0.1; //Mouse sensitivity
	private bool paused = false;
	private Vector3 vel = Vector3.Zero; //vel is saved distinct from Velocity to stop a bug in the slowdown ability 
	private Vector3 starting_pos;
	HUD hud;
	RayCast3D grapple_ray;
	Global_variables Global;

	//~~~~~~Inventory and equipment variables~~~~~~
	enum ArmsEnum {NULL, Grapple, Gun};
	private bool has_grapple = false;
	private bool has_gun = false; 
	private ArmsEnum arm_current = ArmsEnum.NULL;
	public void UnlockAllArms() { has_grapple = true; has_gun = true; arm_current = ArmsEnum.Grapple; } //For debugging

	//~~~~~~Damage variables~~~~~~
	private bool damaged = false; //FIXME - When the damage step is worked on this will need rethinking

	//~~~~~~Jump variables~~~~~~
	const double JUMP_VELOCITY = 20.0; 
	const double JUMP_WINDOW_LENGTH = 0.04; //How long IsJumpJustPressed will record for from the first press. 
	private double jump_last_pressed = 0; //The last time that the jump key was pressed
	private bool IsJumpJustPressed() { return GetTime() <= (jump_last_pressed + JUMP_WINDOW_LENGTH); } //If current time is before when the jump window ends

	//~~~~~~Dodge variables~~~~~~
	private Vector3 dodge = Vector3.Zero; //The direction of dodge (is Vector3.Zero when not dodging)
	const double DODGE_SPEED = 45.0; 
	const double DODGE_DURATION = 0.5; //The length, in seconds that a dodge lasts
	private double dodge_time = 0; //Has delta added to it to track the time that the dodge has lasted. Affected by slowdown
	private int DODGE_MAX = 1; //Number of dodges allowed. Is not a constant so that MAYBE the player could chain more than one dodge at a time.
	private int dodge_left = 1; //The number of dodges left (resets on hitting ground)
    
	//~~~~~~Grapple variables~~~~~~
	enum GrappleEnum {None, Grappling, Falling};
	private GrappleEnum grapple_status = GrappleEnum.None;
	const double GRAPPLE_SPEED = 45.0;
	const double GRAPPLE_REGEN = 5;
	private Area3D grapple_to = null; //Position grappling too
	private Area3D grapple_pass = null; //The inner area of the grapple that will stop the grapple if the player touches
	private Vector3 grapple_vel = Vector3.Zero; //Direction grappling to
	private Area3D grapple_seen = null; //Grapple node being looked at. Used to set set the grapple sprite visibility
	private double grapple_max = 0;

	//~~~~~~Wall run variables~~~~~~
	/*
	enum WallRunEnum {None, Running, Falling};
	private WallRunEnum wall_run_status = WallRunEnum.None;
	const double WALL_RUN_SPEED = 70.0;
	private Area3D running_on = null; //The wall you are running on
	private PathFollow3D wall_path = null;
	*/

	//~~~~~~Slowdown variables~~~~~~
	const double SLOW_DURATION = 5; //Amount of slowdown allowed.
	const double SLOW_VALUE = 0.2; //Amount slowed down
	private double slow_meter = 0.0; //Amount of slowdown currently used
	private bool slow_active = false; //Is slowdown turned on
	private bool slow_runout = false; //When slowdown runs out, it cannot be used until it recharges

	//~~~~~~Gun variables~~~~~~
	const int AMMO_LIMIT = 8;
	private double ammo_left = 8; 
	//private double fire_held = 0; //Records the amount of time that the fire button was held down for the supercharge shot
	//const double FIRE_FORCE = 10; //The basic kickback value.Will be multiplied when the gun is held down. (COULD BE BALANCE ISSUE)

	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

	//Functions section
	
	private static double GetTime() { return DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds; }
	
	private void change_arm(/*int crem*/ ArmsEnum gaining = ArmsEnum.NULL) { 
		//crem is shorthand for crement and is either -1 or 1. It is only used if there's more than 2 arm types.

		//This form of the function is for just grapple and gun. 
		//The player gets the grapple first, so if they don't have it then they can't change arm 
		//Since there's only 2 arms, changing does not need the crem input. If there are more added, then crem will be added 
		switch (gaining) {
			case ArmsEnum.Grapple: hud.PlayArmAnim("gain_grapple"); return;
			case ArmsEnum.Gun:     hud.PlayArmAnim("gain_gun");     return;
		}
		//If not gaining a weapon, then changing a weapon
		if (!has_gun) return; //If don't have the gun yet, then either no arms or only grapple. This is because the gun is the second arm type to unlock (IF THIS CHANGES THEN CHANGE THIS FUNCTION)
		hud.PlayArmAnim("change_" + (arm_current == ArmsEnum.Gun ? "gun" : "grapple"));
	}
	
	public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured; //Will trap the mouse cursor inside the game window
		starting_pos = Position; //For setting the default position that the player should teleport to if OOB

		//Node reference caching
		hud = (HUD)GetNode("CanvasLayer/HUD");
		grapple_ray = (RayCast3D)GetNode("head/Camera3D/GrappleRay");
		
		//For code that teleports the player to a position on scene start using info provided by a previous scene, see scrap code file "Teleport code"

		Camera3D cam = (Camera3D)GetNode("head/Camera3D");
		Global = (Global_variables)GetNode("/root/Global");
		cam.Fov = Global.SavedFOV;

		//Signals and action connections
		hud.FOVChanged += OnFOVHUDChanged;
	}
    
	public override void _Input(InputEvent @event)
    {
        if (!paused) { //This way, if the game is paused, the player cannot turn
			InputEventMouseMotion motion = @event as InputEventMouseMotion;
			if (motion != null) {
				RotateY((float)Mathf.DegToRad(-motion.Relative.X * MOUSE_SEN));
				Node3D head = (Node3D)GetNode("head"); //REMEMBER that caching this would save time but not that much.
				head.RotateX((float)Mathf.DegToRad(-motion.Relative.Y * MOUSE_SEN));
				head.Rotation = new Vector3((float)Mathf.Clamp(head.Rotation.X, Mathf.DegToRad(-89.0), Mathf.DegToRad(89.0)), head.Rotation.Y, head.Rotation.Z);

				//look code adapted from YoutTube video called "How To Code An FPS Controller In Godot 3" by Nagi (https://youtu.be/jf_Hz0diI8Y)
			} 
		}
    }
    
	private void damage_process() { /*This is where the code to process the damage will go*/ }
	
	private bool passed_grapple_point() {
		Area3D player_area = (Area3D)GetNode("body_area"); 
		if (player_area.GetOverlappingAreas().Contains(grapple_pass)) { return true; } //If at the grapple point, stop grappling

		//Use basic algebra:
		//G = grapple_to.Position, P = Position, V = grapple_vel, C = what you're looking for
		//G = P + V*C
		//(G - P)/V = C
		//If C is negative then the player has overshot the grapple point
		Vector3 C = Vector3.Zero;
		C.X = (grapple_to.Position.X - Position.X) / grapple_vel.X;
		C.Y = (grapple_to.Position.Y - Position.Y) / grapple_vel.Y;
		C.Z = (grapple_to.Position.Z - Position.Z) / grapple_vel.Z;
		
		return (C.X < 0 && C.Y < 0 && C.Z < 0);
	}
	
	private void slow_code(double delta) {
		//If slow_runout is true, then don't allow slowdown to be activated again
		//If slowdown meter reaches zero, then slow_runout will be zero;
		//If slowdown is active, increase meter by delta. Otherwise, decrese by delta. Both ways, clamp between 0 and SLOW_DURATION

		if (slow_active) { slow_meter = Mathf.Clamp(slow_meter + delta, 0, SLOW_DURATION); }
		else { slow_meter = Mathf.Clamp(slow_meter - delta, 0, SLOW_DURATION); }

		if (slow_meter == 0) { slow_runout = false; }
		else if (slow_meter == 5) { slow_runout = true; slow_active = false; }

		if (!paused && Input.IsActionJustPressed("slow") && !slow_runout) { slow_active = !slow_active; }

		hud.SetSlowdownGrey(slow_active, slow_meter, SLOW_DURATION, slow_runout);
	}
	
	private void gun_code() {}

	private void detect_grapple() {
		Area3D next = (Area3D)grapple_ray.GetCollider();
		if (next == null || next.EditorDescription != Global.GrappleStr) { next = null; } //This line works because the compiled code checks if the first condition in an OR is true, and if so, does not bother with the other conditions in that OR.
		if (next == grapple_seen) return; //If same as previous frame, no changes needed

		if (grapple_seen != null) { //If current grapple_seen is a grapple point, turn off the sprite and reassign
			((Sprite3D)grapple_seen.GetNode("Sprite3D")).Visible = false; 
			grapple_seen = next; 
		}
		if (next != null) { //If next grapple_seen is a grapple point, reassign and turn on the sprite 
			grapple_seen = next;
			((Sprite3D)grapple_seen.GetNode("Sprite3D")).Visible = true; 
		} 
		hud.SetGrappleDetect(grapple_seen != null);
	}

	private Vector3 velocity_calc(double delta) {
		//The slow code here needs to be implemented on parts of the velocity code that slowly change the value instead of setting it
		//This is things like +=, -=, and lerp statements. 

		//GrapplingEnum.None only happens once the player has touched the normal ground [MAYBE ADD SLIPPY FLOORS LATER]
		if (grapple_status == GrappleEnum.Grappling) {	
			if (paused) { return grapple_vel; }	
			else if (Input.IsActionJustPressed("arm") || Input.IsActionJustPressed("dodge") || passed_grapple_point()) {
				//Cancel grapple and start falling if grapple pressed, dodge pressed, or you've passed the dodge point
				grapple_status = GrappleEnum.Falling;
				grapple_to = null;
				grapple_pass = null;
				grapple_vel = Vector3.Zero;
			} else {
				//If still grappling, return grapple_vel
				return grapple_vel;
			}
		} 

		if (dodge != Vector3.Zero) { 
			//The dodge code is here so that nothing can be done while dodging
			//If this proves to get twisted with the wall running and the slippy floors then it will need changing
			dodge_time += delta;
			if (DODGE_DURATION < dodge_time) { //If dodge is complete
				dodge = Vector3.Zero;
				dodge_time = 0.0;
			} else { //Keep dodging
				return dodge;
			}
		}

		//Here goes the normal controls section
		if (!paused && Input.IsActionJustPressed("arm") && arm_current == ArmsEnum.Grapple && grapple_seen != null) {
			//Can go here even while currently grappling, which means potentially chaining grapples together
			slow_active = false;
			grapple_status = GrappleEnum.Grappling;
			grapple_to = grapple_seen;
			grapple_pass = (Area3D)grapple_seen.GetNode("physical_point");
			grapple_vel = (grapple_seen.Position - Position).Normalized() * (float)GRAPPLE_SPEED;
			grapple_max = Mathf.Clamp(
				new Vector2(grapple_vel.X, grapple_vel.Z).Length(), 
				GRAPPLE_SPEED * 0.2, 
				GRAPPLE_SPEED
			);
			return grapple_vel;
		} 
		
		if (!paused && Input.IsActionJustPressed("dodge")) {
			//The following 2 lines make it so that the momentum is lost once you dodge
			//However it may be a good idea to get rid of these lines because you can throw yourself through the air with the dodge, which could be used for some challenges.
			grapple_status = GrappleEnum.None;
			//wall_run_status = WallRunEnum.None;
			
			move_dir = new Vector3(	
				Input.GetAxis("left","right"), 
				0, 
				Input.GetAxis("forward","backward") 
			).Normalized().Rotated(Vector3.Up, Rotation.Y); 

			if (move_dir == Vector3.Zero) { 
				//If no keys being pressed, then dodge in camera direction
				move_dir = new Vector3(0,0,-1).Rotated(Vector3.Up, Rotation.Y); 
			} 
			dodge = move_dir * (float)DODGE_SPEED;
			dodge_time = 0.0;

			return dodge;
		}

		//Normal walking/falling code
		if (IsOnFloor()) { 
			grapple_status = GrappleEnum.None;
			//wall_run_status = WallRunEnum.None;
			if (vel.Y < 0) { vel.Y = -(float)(GRAVITY * delta); } //This if statement means that if the code glitches and IsOnFloor() returns true the frame after a jump starts, the jump won't be cancelled. But when the player lands on the ground after a jump, the downward momentum will be reset to 0
			if (!paused && /*Input.IsActionJustPressed("jump")*/ IsJumpJustPressed()) {
				vel.Y = (float)(JUMP_VELOCITY);
			}
		} else {
			vel.Y -= (float)(GRAVITY * delta); //Increase gravity
			vel.Y = Mathf.Clamp(vel.Y, (float)TERMINAL_VELOCITY, float.MaxValue); //Have a terminal falling velocity
		}

		move_dir = new Vector3(	
			Input.GetAxis("left","right"), 
			0, 
			Input.GetAxis("forward","backward") 
		).Normalized().Rotated(Vector3.Up, Rotation.Y); 

		if (grapple_status == GrappleEnum.Falling) {// || wall_run_status == WallRunEnum.Falling) {
			/*
			This also applies to the momentum of coming off a wall run
			Have gravity affecting the momentum
			For the X and Z movement, add the key movement to the velocity and reduce it so that its length is less than the highest speed reached. 
			POTENTIALLY, if the player's X and Z velocities reach a low enough level then they will lose all of the momentum and will be sent to GrapplingEnum.None and WallRunEnum.Falling

			This part of the code will require a much more thorough look at Ultrakill.
			
			For the sake of testing the first part of the grappling code, this will end and go to the normal walking and falling code. So delete the following code afterwards
			*/

			if (move_dir != Vector3.Zero) {
				//For the older falling velocity code, see "Old grapple_falling code" in the scrap code folder

				//Basic code that may yield wierd results in the long run
				float y = vel.Y;
				vel.Y = 0;
				float l = vel.Length(); 
				vel += move_dir * (float)(2 * SPEED * delta); //Multiplied because SPEED * delta was way too slow
				if (l > grapple_max) { vel = vel.Normalized() * (float)GRAPPLE_SPEED; }
				/*
				//This code meant that you'd lose all of your speed if you moved wrong
				//I removed it because I prefered the ability to glide after a grapple, wall run, etc
				//HOWEVER, gliding may be an exploitable move allowing the player to skip past the punishment
				else if (l <= SPEED) {
					grapple_status = GrappleEnum.None;
					wall_run_status = WallRunEnum.None;
				}*/
				vel.Y = y;
			}
		} 
		else {
			//The lerp statements are fine for now but may spell trouble later
			vel.X = Mathf.Lerp(vel.X, move_dir.X * (float)SPEED, (float)(ACCEL * delta));
			vel.Z = Mathf.Lerp(vel.Z, move_dir.Z * (float)SPEED, (float)(ACCEL * delta));
		}
		
		return vel;
	}

	private void TestingGettingCollidedObject() {
		//This function is here to test how to grab the object that the player's collision shape is hitting.
		//If this is possible, it would be used in testing if the player has hit jumpable walls or damaging surfaces.
		
		//First you have to grab the number of collisions that have happened since the last MoveAndSlide() call
		int number = GetSlideCollisionCount(); 
		//You grab the all the collider info from the main node (the CharacterBody3D)
		List<StaticBody3D> data = new List<StaticBody3D>(number);
		string info_to_display = number.ToString();
		for (int i = 0; i < number; i++) { 
			data.Add((StaticBody3D)GetSlideCollision(i).GetCollider()); 
			info_to_display += "\n" + data[i].EditorDescription;
		}
		hud.DebugText(info_to_display);
	}

	public override void _PhysicsProcess(double delta)
    {
		//hud.SetGrappleRayFeedback(grapple_ray);

		if (Input.IsActionJustPressed("jump") && !paused) { jump_last_pressed = GetTime(); } //For updating IsJumpJustPressed()
		//Debug cheat code
		if (Input.IsActionPressed("debug1") && Input.IsActionPressed("debug2") && Input.IsActionPressed("debug3")) { 
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GetTree().ChangeSceneToFile("res://scenes/debug_menu.tscn"); 
			return;
		} 
		//Fullscreen code
		if (Input.IsActionJustPressed("toggle_fs")) {
			if (GetWindow().Mode == Window.ModeEnum.Fullscreen) {
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			} else {
				DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			}
		}

		//OOB cancel code
		if (Position.Y < -200) { Position = starting_pos; }
		//Pause input code
		if (Input.IsActionJustPressed("pause")) {
			hud.TogglePause();
			paused = !paused;
			//true: 2-(2*1) = visible mouse | false: 2-(2*0) = captured mouse
			Input.MouseMode = (Input.MouseModeEnum)(paused ? 0 : 2); //Hail int to enum casting
		}

		//-----------------------------
		//Movement code section. The pause does not stop the movement but it does open the menu and stops mouse and key inputs
		//-----------------------------

		detect_grapple();

		UpDirection = Vector3.Up;
		slow_code(delta);
		
		vel = velocity_calc(delta * (slow_active ? SLOW_VALUE : 1));

		//GD.Print(vel.Y);
		//hud.DebugText("Vel.Y = " + vel.Y.ToString() + "\nPos.Y = " + Position.Y.ToString() + "\nIsOnFloor() = " + IsOnFloor());

		Velocity = vel * (float)(slow_active ? SLOW_VALUE : 1);

		MoveAndSlide();
    }

	//Actions code
	private void OnFOVHUDChanged(float value)
	{
		Camera3D cam = (Camera3D)GetNode("head/Camera3D");
		cam.Fov = value;
	}
}