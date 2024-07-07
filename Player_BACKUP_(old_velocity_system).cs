using Godot;
using System;
using System.Collections.Generic;

/*Todo section
TODO - Runnable walls will be implemented by having the player run along any staticbody3D that has a horizontal slant.

TODO - Implement a sliding mechanic where the player can slide but then end the slide and jump and conserve their momentum. Should maybe have a delay to when the slide ends and the player can jump (like with the jump buffer, but a "slide buffer")

TODO - Need a better control scheme. The issue is that you can't map shoot, grapple, and slowdown to the mouse. Other solutions are:
  ~~Shoot and grapple should be on left and right mouse respectively, but doing that means that I can't have an alt-fire for the gun unless the alt-fire is attached to holding the button.
  ~~I could make the grapple and the gun seperate abilities which can be cycled between, but that means that challenges that require shooting things and grappling are harder to play (if done right, it could be like Onnamusha, the Furi character)

TODO - Implement the outlines of the grapple icon in the HUD, and the outline sprite of the grapple point being pointed at. The basic idea is to save the node that was looked at on the previous physics step (if there was none, it will equal null because of the "as Area3D" part). Then, when the player stops looking at the grapple point ('current grapple != previous grapple' will catch null or a different grapple point), the code will set the outline sprite of that point to be invisible. This also can be simplified because if the player is looking at a grapple point on that frame, then the outline sprite can be set to invisible. (WILL probably need to filter out other Area3D nodes that aren't grapples btw)

TODO - Have a think about how the ability recharges would go and how they work with the "big punishment angle". Will need to discuss this in a larger document.

TODO - Really need to get around to properly implementing the grapple icon in the bottom of the screen, as well as the cooldown
This means adding the cooldown code (just copy the slow code) but it also means having it tell you when a grapple point is within reach.

TODO - Need to start thinking about the gun. Let's break that down
  ~~Implementing the gun's functionality: ammo count, reload animation that's smaller depending on the amount of ammo used, and HUD ammo count (might be a good idea to have it be a segmented block like the Crash Bandicoot health bars)
  ~~The visual design and structure of the model
  ~~Thinking about the animations (it needs the battery that pings out, and I need to think about the battery colour animation and how that would work with potentially having different length) 
  ~~Think of how it would work with the platforming challenges. Obviously include breakable walls that would regenerate, but also it would be fun to be able to use the gun as a possible double jump (the movement code could be a nightmare tho) 

TODO - Would be better to have some simple but obvious icons for the abilities (slowdown, grapple, dodge, gun) so that I can track what means what and simply swap out the shitty textures with better textures when the game is nearing completion.
*/

public partial class Player_BACKUP : CharacterBody3D
{
	//To check the number of variants of an enum, use Enum.GetNames(typeof(TheEnum)).Length;
	const double SPEED = 15.0; 
	const double ACCEL = 80.0; 
	const double FRICTION = 80.0;
	const double GRAVITY = 30.0; 
	const double TERMINAL_VELOCITY = -120.0;
	private double fall_max = 0; //Current maximum forwards momentum during StatesEnum.Falling
	private Vector3 move_dir = Vector3.Zero; //Move direction (derived from inputs)
	const double MOUSE_SEN = 0.1; //Mouse sensitivity
	private bool paused = false;
	private Vector3 vel = Vector3.Zero; //vel is saved distinct from Velocity to stop the slowdown mechanic overwriting velocity and ending
	private Vector3 starting_pos;
	enum StatesEnum {Normal, Falling, Grappling, WallRun, Animation}
	private StatesEnum player_state = StatesEnum.Normal;
	HUD hud;
	RayCast3D grapple_ray;
	Global_variables Global;

	//~~~~~~Inventory and equipment variables~~~~~~
	enum ArmsEnum {NULL, Grapple, Gun}
	private bool has_grapple = false;
	private bool has_gun = false; 
	private ArmsEnum arm_current = ArmsEnum.NULL;
	public void UnlockAllArms() { has_grapple = true; has_gun = true; arm_current = ArmsEnum.Grapple; } 
	const double AMR_BUFFER = 0.1;
	//TODO - Need to add an input-buffer-like system where if the arm button is pressed then a window appears where the grapple can be used 

	//~~~~~~Damage variables~~~~~~
	private bool damaged = false; //FIXME - When the damage step is worked on this will need rethinking

	//~~~~~~Jump variables~~~~~~
	const double JUMP_VELOCITY = 20.0; 
	const double JUMP_BUFFER = 0.1; 
	private double jump_last_pressed = 0; 
	private bool IsJumpJustPressed() { return GetTime() <= jump_last_pressed + JUMP_BUFFER; } //If current time is before when the jump window ends
	const double COYOTE_TIME = 0.1; 
	private double last_on_floor = 0;
	private bool WasOnFloor() { return GetTime() <= last_on_floor + COYOTE_TIME; } 

	//~~~~~~Dodge variables~~~~~~
	private Vector3 dodge = Vector3.Zero; //The direction of dodge (is Vector3.Zero when not dodging)
	const double DODGE_SPEED = 45.0; 
	const double DODGE_DURATION = 0.4; //The length, in seconds that a dodge lasts
	private double dodge_time = 0; //Has delta added to it to track the time that the dodge has lasted. Affected by slowdown
	private int DODGE_MAX = 1; //Number of dodges allowed. Is not a constant so that MAYBE the player could chain more than one dodge at a time.
	private int dodge_left = 1; //The number of dodges left (resets on hitting ground)
    
	//Sliding variables
	private Vector3 slide = Vector3.Zero; //If equals Vector3.Zero then not sliding (SUBJECT TO CHANGE)
	const double SLIDE_START_SPEED = 30;
	const double SLIDE_END_SPEED = 6;
	private Vector3 slide_change = Vector3.Zero; //Factor by which the slide speed changes every frame. Will be different dependent on slope of floor
	//TODO - Start programming the slopped floor stuff and slopped floor geometry after the basic version of the slide.
	const double SLIDE_BUFFER = 0.1; //TODO - Add a cooldown to the slide activation. 
	private double slide_last_pressed = 0; //TODO - Need to remember that, after a slide has been activated, this number needs to be set to zero as to not cause confusion by accidently deactivating the slide on the frame after activation. 
	private bool IsSlideJustPressed() { return GetTime() <= slide_last_pressed + SLIDE_BUFFER; }
	//TODO - Joe's answers about the slide system are very informative and shall be catalogued here
	//1. Start sliding while the player is moving very fast? - Keep the speed but reduce it at the same rate as the normal slide reduces.
	//2. Player slides until their speed is really low? - Have the slide turn off once the player hits a certain threshold
	//3. Controls for the slide? - It should be a toggle (so you can have a buffer where the player can start a slide immediately after pressing any button)
	//ALSO, when sliding down a slope, calculate the top speed of the slide, accelerate to that level, and then cap the speed at that level (ACCELERATION OF THIS WILL NEED TO BE THOUGHT ABOUT). Since there should be slippery floors, there should also be simple slopes for the player to walk on, and also slide on for extra speed 
	
	//~~~~~~Grapple variables~~~~~~
	const double GRAPPLE_SPEED = 45.0;
	const double GRAPPLE_REGEN = 5;
	private Area3D grapple_to = null; //Position grappling too
	private Area3D grapple_pass = null; //The inner area of the grapple that will stop the grapple if the player touches
	private Vector3 grapple_vel = Vector3.Zero; //Grapple direction
	private Area3D grapple_seen = null; //Grapple node being looked at. Used to set set the grapple sprite visibility

	//~~~~~~Wall run variables~~~~~~
	
	const double WALL_RUN_SPEED = 70.0;
	private Area3D running_on = null; //The wall you are running on
	

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
    
	private void interactions_code() { //This function is for processing the player's use of the interact button
		if (!Input.IsActionJustPressed("interact")) return;
		//Proper interact code goes below 
	}
	
	private void damage_process() { //This is where the code to process the damage will go 
	}
	
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
		
		return C.X < 0 && C.Y < 0 && C.Z < 0;
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

	private double H_vel() { return Mathf.Sqrt((vel.X * vel.X) + (vel.Z * vel.Z )); }

	private bool IsDirectionJustPressed() { return Input.IsActionJustPressed("forward") || Input.IsActionJustPressed("back") || Input.IsActionJustPressed("left") || Input.IsActionJustPressed("right"); }
	
	private Vector3 velocity_calc(double delta) {
		//The slow code here needs to be implemented on parts of the velocity code that slowly change the value instead of setting it
		//This is things like +=, -=, and lerp statements. 

		//GrapplingEnum.None only happens once the player has touched the normal ground [MAYBE ADD SLIPPY FLOORS LATER]
		if (player_state == StatesEnum.Grappling) {	
			if (paused) { return grapple_vel; }	
			else if (Input.IsActionJustPressed("arm") || Input.IsActionJustPressed("dodge") || passed_grapple_point()) {
				//Cancel grapple and start falling if grapple pressed, dodge pressed, or you've passed the dodge point
				player_state = StatesEnum.Falling;
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

		if (slide != Vector3.Zero) { 
			if (Input.IsActionJustPressed("dodge") || IsJumpJustPressed() || !WasOnFloor() || IsDirectionJustPressed()) { 
				slide = Vector3.Zero;
				player_state = StatesEnum.Falling;
				fall_max = H_vel();
				((AnimationPlayer)GetNode("player_anim")).PlayBackwards("slide_view");
			}
			else {
				slide_change = slide.Normalized() / (float)(FRICTION * delta);
				slide -= slide_change;
				if (slide.Length() <= SLIDE_END_SPEED) {
					slide = Vector3.Zero;
					player_state = StatesEnum.Normal;
					((AnimationPlayer)GetNode("player_anim")).PlayBackwards("slide_view");
				} else return slide;
			}
		}
		
		if (!paused && Input.IsActionJustPressed("arm") && arm_current == ArmsEnum.Grapple && grapple_seen != null) {
			//Can go here even while currently grappling, which means potentially chaining grapples together
			slow_active = false;
			player_state = StatesEnum.Grappling;
			last_on_floor = 0; //Fixes glitch where close-by grapple points attached to vertically set player_state to normal
			//TODO - Test jumping right before grappling for any glitches
			grapple_to = grapple_seen;
			grapple_pass = (Area3D)grapple_seen.GetNode("physical_point");
			grapple_vel = (grapple_seen.Position - Position).Normalized() * (float)GRAPPLE_SPEED; //TODO - This needs an extra bit to it where the player's momentum is conserved if they grapple towards it at a higher speed than GRAPPLE_SPEED. Probably calculated by adding the grapple_vel value to the current velocity and then changing the multiplied value 
			fall_max = Mathf.Clamp( //TODO - Need to change this to falling_max at some point and implement it for the slide and the dodge
				new Vector2(grapple_vel.X, grapple_vel.Z).Length(), 
				GRAPPLE_SPEED * 0.2, //TODO - The 0.2 value is subject after the tall jump off test area is built
				GRAPPLE_SPEED
			);
			return grapple_vel;
		} 
		
		if (!paused && Input.IsActionJustPressed("dodge")) {
			//The following 2 lines make it so that the momentum is lost once you dodge
			//However it may be a good idea to get rid of these lines because you can throw yourself through the air with the dodge, which could be used for some challenges.
			player_state = StatesEnum.Normal;
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

		if (!paused && IsSlideJustPressed() && WasOnFloor()) { //Activate the slide
			//The button does not need a cooldown because if the player mashes the slide button, then it's very likely that they will cancel the slide and lose speed. 
			//Activating the slide should not overwrite the player_state
			slide_last_pressed = 0;

			((AnimationPlayer)GetNode("player_anim")).Play("slide_view");

			move_dir = new Vector3(	
				Input.GetAxis("left","right"), 
				0, 
				Input.GetAxis("forward","backward") 
			).Normalized().Rotated(Vector3.Up, Rotation.Y); 

			slide = move_dir * (float)(H_vel() > SLIDE_START_SPEED ? H_vel() : SLIDE_START_SPEED);
			//Set slide_change in the "is sliding" section of this function
			return slide;
		}

		//Normal walking/falling code
		if (WasOnFloor()) { 
			if (vel.Y < 0) { vel.Y = -(float)(GRAVITY * delta); } //This if statement means that if the code glitches and IsOnFloor() returns true the frame after a jump starts, the jump won't be cancelled. But when the player lands on the ground after a jump, the downward momentum will be reset to the first frame of applied gravity. This is to ensure that IsOnFloor() doesn't have the flickering issue, but alos so that the player won't have tons of downwards momentum if they step off a platform. This all works because you won't be on the floor if you are moving up.
			if (!paused && IsJumpJustPressed()) {
				vel.Y = (float)JUMP_VELOCITY;
				//Reset the 2 counters in order to not jump twice 
				jump_last_pressed = 0; 
				last_on_floor = 0; 
			} //GD.Print("vel = " + vel.ToString() + " | vel.Y = " + vel.Y.ToString());
		} 
		else {
			vel.Y = Mathf.Clamp(vel.Y - (float)(GRAVITY * delta), (float)TERMINAL_VELOCITY, float.MaxValue); //Apply gravity up to terminal falling velocity
		}

		move_dir = new Vector3(	
			Input.GetAxis("left","right"), 
			0, 
			Input.GetAxis("forward","backward") 
		).Normalized().Rotated(Vector3.Up, Rotation.Y); 

		if (player_state == StatesEnum.Falling) {
			//For the older falling velocity code, see "Old grapple_falling code" in the scrap code folder
			vel += move_dir * (float)(SPEED * delta); //Multiplied because SPEED * delta was way too slow
			if (H_vel() > fall_max) { 
				//TODO - The only way to properly test this system of having a velocity limit in falling is to design a test area with a long fall
				Vector3 new_h_dir = new Vector3(vel.X, 0, vel.Z).Normalized() * (float)fall_max;
				vel.X = new_h_dir.X;
				vel.Z = new_h_dir.Z;
			}
			if (WasOnFloor()) { 
				vel = vel.MoveToward( new(0, vel.Y, 0), (float)(FRICTION * delta));
				if (vel.Length() <= SPEED) player_state = StatesEnum.Normal;
			}
			//hud.AddDebugText("vel X and Z = (" + vel.X.ToString() + "," + vel.Z.ToString() + ")");
		} 
		else {
			Vector3 speed_target;
			float coef = (float)(ACCEL * delta);
			if (move_dir.IsZeroApprox()) { //If not trying to move, slowdown due to friction
				speed_target = new(0, vel.Y, 0); //This is equal to no horizontal speed with unaffected vertical speed
			} 
			else {
				speed_target = move_dir * (float)SPEED;
				speed_target.Y = vel.Y;
			}
			vel = vel.MoveToward(speed_target, coef);
		} 
		return vel;
	}

	private void TestingGettingCollidedObject() {
		//This function is here to test how to grab the object that the player's collision shape is hitting.
		//If this is possible, it would be used in testing if the player has hit jumpable walls or damaging surfaces.
		
		//First you have to grab the number of collisions that have happened since the last MoveAndSlide() call
		int number = GetSlideCollisionCount(); 
		//You grab the all the collider info from the main node (the CharacterBody3D)
		List<StaticBody3D> data = new (number);
		string info_to_display = number.ToString();
		for (int i = 0; i < number; i++) { 
			data.Add((StaticBody3D)GetSlideCollision(i).GetCollider()); 
			info_to_display += "\n" + data[i].EditorDescription;
		}
		//hud.DebugText(info_to_display);
	}

	public override void _PhysicsProcess(double delta)
    {
		//GD.Print("\nNew physics step");
		//hud.SetGrappleRayFeedback(grapple_ray);
		hud.ClearDebugText();

		if (Input.IsActionJustPressed("jump") && !paused) { jump_last_pressed = GetTime(); } //For updating IsJumpJustPressed()
		if (Input.IsActionJustPressed("slide") && !paused) { slide_last_pressed = GetTime(); } //For updating IsSlideJustPressed()
		if (IsOnFloor()) { last_on_floor = GetTime(); } //For updating WasOnFloor()

		//Debug cheat code 
		if (Input.IsActionPressed("debug1") && Input.IsActionPressed("debug2") && Input.IsActionPressed("debug3")) { 
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GetTree().ChangeSceneToFile("res://scenes/debug_menu.tscn"); 
			return;
		} 
		//TODO - When the game is close to release, delete these lines because they're only for debugging
		//Fullscreen code (for debugging)
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
			//0 = visible mouse | 2 = captured mouse 
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
		//hud.AddDebugText("Empty line");
		hud.AddDebugText("WasOnFloor() = " + WasOnFloor());
		hud.AddDebugText(player_state.ToString());
		MoveAndSlide();
		//GD.Print("vel = " + vel);
    }

	//Actions code
	private void OnFOVHUDChanged(int value)
	{
		Camera3D cam = (Camera3D)GetNode("head/Camera3D");
		cam.Fov = value;
	}
}