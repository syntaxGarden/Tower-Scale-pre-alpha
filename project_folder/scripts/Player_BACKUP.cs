using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

/*Todo section
TODO - Runnable walls will be implemented by having the player run along any staticbody3D that has a horizontal slant.

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
	const double FALL_MOVE_FACTOR = 1;
	private double fall_max = 0; //Current maximum forwards momentum during StatesEnum.Falling
	private Vector3 move_dir = Vector3.Zero; //Move direction (derived from inputs)
	const double MOUSE_SEN = 0.1; //Mouse sensitivity
	private int FOVValue = 90;
	[Export]
	private float FOVInfluence = 0;
	private bool paused = false;
	private Vector3 vel_stored = Vector3.Zero; //vel is saved distinct from Velocity to stop the slowdown mechanic overwriting velocity and ending
	private Vector3 starting_pos;
	enum StatesEnum {Normal, Falling, Grappling, WallRun, Dodging, Sliding, Stunned, Animation}
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
	//TODO - Need to add an input-buffer-like system where if the arm button is pressed then a window appears where the grapple can be used, but there needs to be a thing in the code 

	//~~~~~~Jump variables~~~~~~
	const double JUMP_VELOCITY = 20.0; 
	const double JUMP_BUFFER = 0.1; 
	private double jump_last_pressed = 0; 
	private bool IsJumpJustPressed() { return GetTime() <= jump_last_pressed + JUMP_BUFFER; } //If current time is before when the jump window ends
	const double COYOTE_TIME = 0.1; 
	private double last_on_floor = 0;
	private bool WasOnFloor() { return GetTime() <= last_on_floor + COYOTE_TIME; } 

	//~~~~~~Dodge variables~~~~~~
	private Vector3 dodge_dir = Vector3.Zero; //The direction of dodge (is Vector3.Zero when not dodging)
	const double DODGE_SPEED = 60.0; 
	const double DODGE_DURATION = 0.3; //The length, in seconds that a dodge lasts
	const double DODGE_I_FRAMES = 0.25; //The length of time that the 
	private double dodge_start = 0; 
	const double DODGE_COOLDOWN_LENGTH = 5; //Time, in seconds, that the dodge cooldown will last
	const double DODGE_GROUNDED_RECHARGE = 5; //Multiplicity amount of dodge recharge on the ground
	private double dodge_recharge_level = DODGE_COOLDOWN_LENGTH; //Level at which the dodge has recharged 
	private bool IsDodgeReady() { return dodge_recharge_level == DODGE_COOLDOWN_LENGTH && player_state != StatesEnum.Dodging; } //TODO - Test this function
    
	//~~~~~~Sliding variables~~~~~~
	private Vector3 slide_dir = Vector3.Zero; //If equals Vector3.Zero then not sliding (SUBJECT TO CHANGE)
	const double SLIDE_START_SPEED = 30;
	const double SLIDE_END_SPEED = 2;
	private Vector3 slide_change = Vector3.Zero; //Factor by which the slide speed changes every frame. Will be different dependent on slope of floor
	//TODO - Start programming the slopped floor stuff and slopped floor geometry after the basic version of the slide.
	const double SLIDE_BUFFER = 0.1;
	private double slide_last_pressed = 0; 
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
	private void change_arm(/*int crem*/ ArmsEnum gaining = ArmsEnum.NULL) 
	{ 
		//crem is shorthand for crement and is either -1 or 1. It is only used if there's more than 2 arm types.

		//This form of the function is for just grapple and gun. 
		//The player gets the grapple first, so if they don't have it then they can't change arm 
		//Since there's only 2 arms, changing does not need the crem input. If there are more added, then crem will be added 
		switch (gaining) 
		{
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
		FOVValue = Global.SavedFOV;

		//Signals and action connections
		hud.FOVChanged += OnFOVHUDChanged;
	}
    
	public override void _Input(InputEvent @event)
    {
        if (!paused)  //This way, if the game is paused, the player cannot turn
		{
			InputEventMouseMotion motion = @event as InputEventMouseMotion;
			if (motion != null) 
			{
				RotateY((float)Mathf.DegToRad(-motion.Relative.X * MOUSE_SEN));
				Node3D head = (Node3D)GetNode("head"); //REMEMBER that caching this would save time but not that much.
				head.RotateX((float)Mathf.DegToRad(-motion.Relative.Y * MOUSE_SEN));
				head.Rotation = new Vector3((float)Mathf.Clamp(head.Rotation.X, Mathf.DegToRad(-89.0), Mathf.DegToRad(89.0)), head.Rotation.Y, head.Rotation.Z);

				//look code adapted from YoutTube video called "How To Code An FPS Controller In Godot 3" by Nagi (https://youtu.be/jf_Hz0diI8Y)
			} 
		}
    }
    
	private void interactions_code() 
	{
		//This function is for processing the player's use of the interact button
		if (!Input.IsActionJustPressed("interact")) return;
		//Proper interact code goes below 
	}
	
	private void damage_process()  
	{
		//This is where the code to process the damage will go 
	}
	
	private bool passed_grapple_point() 
	{
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
	
	private void slow_code(double delta)
	{
		//If slow_runout is true, then don't allow slowdown to be activated again
		//If slowdown meter reaches zero, then slow_runout will be zero;
		//If slowdown is active, increase meter by delta. Otherwise, decrese by delta. Both ways, clamp between 0 and SLOW_DURATION

		if (slow_active) { slow_meter = Mathf.Clamp(slow_meter + delta, 0, SLOW_DURATION); }
		else { slow_meter = Mathf.Clamp(slow_meter - delta, 0, SLOW_DURATION); }

		if (slow_meter == 0) { slow_runout = false; }
		else if (slow_meter == 5) { slow_runout = true; slow_active = false; }

		if (!paused && Input.IsActionJustPressed("slow") && !slow_runout) { slow_active = !slow_active; }

		hud.SetSlowdownGrey(slow_active, slow_meter, SLOW_DURATION, slow_runout);
		((AnimationPlayer)GetNode("player_anim")).SpeedScale = slow_active ? 0.5f : 1f;
	}
	
	private void gun_code() 
	{
		if (arm_current != ArmsEnum.Gun) return;
	}

	private void detect_grapple() 
	{
		Area3D next = (Area3D)grapple_ray.GetCollider();
		if (next == null || next.EditorDescription != Global.GrappleStr) { next = null; } //This line works because the compiled code checks if the first condition in an OR is true, and if so, does not bother with the other conditions in that OR.
		if (next == grapple_seen) return; //If same as previous frame, no changes needed

		if (grapple_seen != null)  //If current grapple_seen is a grapple point, turn off the sprite and reassign
		{
			((Sprite3D)grapple_seen.GetNode("Sprite3D")).Visible = false; 
			grapple_seen = next; 
		}
		if (next != null) //If next grapple_seen is a grapple point, reassign and turn on the sprite 
		{
			grapple_seen = next;
			((Sprite3D)grapple_seen.GetNode("Sprite3D")).Visible = true; 
		} 
		hud.SetGrappleDetect(grapple_seen != null); //TODO - 	GET GRAPPLE ICON so you can finish this function in HUD.cs
	}

	private double H_vel() { return Mathf.Sqrt((vel_stored.X * vel_stored.X) + (vel_stored.Z * vel_stored.Z)); }

	private bool IsDirectionJustPressed() { return Input.IsActionJustPressed("forward") || Input.IsActionJustPressed("backward") || Input.IsActionJustPressed("left") || Input.IsActionJustPressed("right"); }
	
	
	/////////////////////////////////////////////////////////////////////////////////////////
	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~Velocity Section~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	/////////////////////////////////////////////////////////////////////////////////////////
	
	private Vector3 velocity_calc(double delta) 
	{
		if (player_state == StatesEnum.Animation) return Vector3.Zero;
		Vector3 vel = vel_stored;
		bool end_early = false;
		vel = ProcessMovementControls(vel, delta, ref end_early);
		if (end_early) return vel;
		return ActualMovements(vel, delta);
	}
	private Vector3 ProcessMovementControls(Vector3 vel, double delta, ref bool end_early) 
	{
		//Pause turns off inputs and stun is uninterruptable. Therefore, if they happen, don't process inputs
		if (paused || player_state == StatesEnum.Stunned) return vel; 
		//TODO - Let the player grapple or slide out of a dodge because that gives the player more control

		if (Input.IsActionJustPressed("arm") && arm_current == ArmsEnum.Grapple) 
		{
			if (player_state == StatesEnum.Grappling) 
			{
				//if (grapple_seen != null && grapple_seen != grapple_to)  { end_early = true; return SetGrapple(true); } else { return SetGrapple(false); }
				//If hitting new, unique grapple, change to that grapple point. End early
				//If hitting no grapple point or same grapple point, cancel grapple, start fall. Don't end early 
				bool cond = grapple_seen != null && grapple_seen != grapple_to;
				end_early = cond;
				if (cond) { return StartGrapple(); } else { StopGrapple(); } 
			}
			else 
			{
				if (grapple_seen != null /*TODO -	GRAPPLE COOLDOWN*/) //If a new grapple point is seen and not on cooldown
				{
					switch (player_state) 
					{
						case StatesEnum.Falling: StopFall(); break; 
						case StatesEnum.WallRun: StopWallRun(); break; 
						case StatesEnum.Sliding: StopSlide(); break;  
					}
					end_early = true;
					return StartGrapple();
				}
				else 
				{
					GD.Print("Grapple cooldown starts here");
					//TODO - 	GRAPPLE COOLDOWN
				}
			}
		}
		else if (Input.IsActionJustPressed("dodge")) 
		{ 
			if (player_state == StatesEnum.Dodging /*TODO - 	DODGE COOLDOWN*/)
			{
				GD.Print("Already dodging or on cooldown");
			}
			else 
			{
				switch(player_state) 
				{
					case StatesEnum.Falling:   StopFall(); break;
					case StatesEnum.Grappling: StopGrapple(); break;
					case StatesEnum.WallRun:   StopWallRun(); break;
					case StatesEnum.Sliding:   StopSlide(); break;
				}
				end_early = true;
				return StartDodge();
			}
		}
		else if (IsSlideJustPressed()) 
		{
			switch (player_state) 
			{
				case StatesEnum.Normal:
				case StatesEnum.Falling:
					if (!WasOnFloor()) return vel;
					fall_max = 0;
					end_early = true;
					return StartSlide();
				case StatesEnum.Grappling: StopGrapple(); break;
				case StatesEnum.WallRun:   StopWallRun(); break;
				case StatesEnum.Sliding:   StopSlide();   break;
			}
		}
		return vel;
	}
	private Vector3 ActualMovements(Vector3 vel, double delta) 
	{
		switch (player_state)
		{
			case StatesEnum.Grappling: 
				if (passed_grapple_point()) { StopGrapple(); break; }
				else { return grapple_vel; }

			case StatesEnum.WallRun: 
				//TODO - 	WALL RUNNING
				//If not touching wall, start falling in the direction of the wall run
				//Else, calculate the direction that the player should be wall running, return that velocity
				break;

			case StatesEnum.Dodging:
				if (GetTime() < dodge_start + DODGE_DURATION ) { return dodge_dir; }
				else { vel = StopDodge(); break; }

			case StatesEnum.Sliding: 
				if (!WasOnFloor() || (!paused && (IsJumpJustPressed() || IsDirectionJustPressed()))) { StopSlide(); }
				else 
				{
					slide_change = slide_dir.Normalized() * (float)(FRICTION * -0.2 * delta); //TODO - 	SLOPED FLOORS 
					slide_dir += slide_change;
					if (slide_dir.Length() <= SLIDE_END_SPEED) { StopSlide(); } 
					else { return slide_dir; }
				}
				break;

			case StatesEnum.Stunned: 
				//TODO - 	STUN STATE
				break;
		}

		if (WasOnFloor()) 
		{ 
			if (vel.Y < 0) { vel.Y = -(float)(GRAVITY * delta); } //See "jump clip fix" in scrap_code file for details
			if (!paused && IsJumpJustPressed()) 
			{
				//TODO - Need to think about a system where, if the player jumps against the current direction, that jump should allow them to override their velocity and redirect themselves (give the player more fun control)
				vel.Y = (float)JUMP_VELOCITY;
				//Reset the 2 counters in order to not jump twice 
				jump_last_pressed = 0; 
				last_on_floor = 0; 
				//TODO - Start implementing the stronger down gravity, the shorter jump on space release, and other systems that make a more usable jump.
			} 
		} 
		else { vel.Y = Mathf.Clamp(vel.Y - (float)(GRAVITY * delta), (float)TERMINAL_VELOCITY, float.MaxValue); } //Apply gravity up to terminal falling velocity

		move_dir = new Vector3(	
			Input.GetAxis("left","right"), 
			0, 
			Input.GetAxis("forward","backward") 
		).Normalized().Rotated(Vector3.Up, Rotation.Y); 
		
		if (player_state == StatesEnum.Falling) 
		{
			//For the older falling velocity code, see "Old grapple_falling code" in the scrap code folder
			vel += move_dir * (float)(SPEED * FALL_MOVE_FACTOR * delta); //FALL_MOVE_FACTOR increases the influence of the movement keys to give the playe more mid-air control
			if (H_vel() > fall_max) 
			{ 
				//TODO - The only way to properly test this system of having a velocity limit in falling is to design a test area with a long fall
				Vector3 new_h_dir = new Vector3(vel.X, 0, vel.Z).Normalized() * (float)fall_max;
				vel.X = new_h_dir.X;
				vel.Z = new_h_dir.Z;
			}
			if (WasOnFloor()) 
			{ 
				vel = vel.MoveToward( new(0, vel.Y, 0), (float)(FRICTION * delta));
				if (vel.Length() <= SPEED) { player_state = StatesEnum.Normal; return vel; }
			} 
		}

		if (player_state == StatesEnum.Normal)
		{
			//The following code is an evolution of the code shown in YouTube video "Make an Action RPG in Godot 3.2 (P3 | collisions + move_and_slide)" by Heartbeast
			//Link = https://www.youtube.com/watch?v=TQKXU7iSWUU
			Vector3 speed_target;
			float coef = (float)((move_dir.IsZeroApprox() ? FRICTION : ACCEL) * delta);
			if (move_dir.IsZeroApprox())  //If not trying to move, slowdown due to friction
			{
				speed_target = new(0, vel.Y, 0); //This is equal to no horizontal speed with unaffected vertical speed
			} 
			else 
			{
				speed_target = move_dir * (float)SPEED;
				speed_target.Y = vel.Y;
			}
			vel = vel.MoveToward(speed_target, coef); 
			//TODO - Consider changing this to a lerp for the end of the dodge move, but ONLY for normal.
}

		return vel;
	}
	private void StartFall(Vector3 original)
	{
		player_state = StatesEnum.Falling;
		fall_max = new Vector2(original.X, original.Z).Length();
	}
	private void StopFall() { fall_max = 0; }
	private Vector3 StartGrapple() 
	{
		slow_active = false; 
		last_on_floor = 0;
		player_state = StatesEnum.Grappling;
		grapple_to = grapple_seen;
		grapple_pass = (Area3D)grapple_seen.GetNode("physical_point");
		grapple_vel = (grapple_seen.Position - Position).Normalized() * (float)GRAPPLE_SPEED; //TODO - 	CONSERVE MOMENTUM
		return grapple_vel;
	}
	private void StopGrapple() 
	{
		StartFall(grapple_vel);
		grapple_to =  null;
		grapple_pass = null;
		grapple_vel =  Vector3.Zero;
	}
	private Vector3 StartDodge() 
	{
		player_state = StatesEnum.Dodging;
		dodge_start = GetTime(); 
		move_dir = new Vector3(	
			Input.GetAxis("left","right"), 
			0, 
			Input.GetAxis("forward","backward") 
		).Normalized().Rotated(Vector3.Up, Rotation.Y); 
		if (move_dir == Vector3.Zero) move_dir = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y); //If no direction keys, dodge in direction being faced
		dodge_dir = move_dir * (float)DODGE_SPEED;
		return dodge_dir;
	}
	private Vector3 StopDodge()
	{
		player_state = StatesEnum.Normal;
		dodge_start = 0;
		Vector3 stored = dodge_dir;
		dodge_dir = Vector3.Zero;
		return stored.Normalized() * (float)SPEED;
	}
	private Vector3 StartSlide() 
	{
		player_state = StatesEnum.Sliding;
		slide_last_pressed = 0;
		((AnimationPlayer)GetNode("player_anim")).Play("slide_view");

		move_dir = new Vector3(	
			Input.GetAxis("left","right"), 
			0, 
			Input.GetAxis("forward","backward") 
		).Normalized().Rotated(Vector3.Up, Rotation.Y); 
		if (move_dir == Vector3.Zero) move_dir = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y); //If no direction keys, slide in direction being faced
		slide_dir = move_dir * (float)(H_vel() > SLIDE_START_SPEED ? H_vel() : SLIDE_START_SPEED); 
		//TODO - 	CONSERVE MOMENTUM | Specifically from a long fall 

		//TODO - Remember that if the player slides to the side on a slope, that they should turn towards the bottom of the as they slide 

		return slide_dir;
	}
	private void StopSlide()
	{
		StartFall(slide_dir);
		slide_last_pressed = 0;
		((AnimationPlayer)GetNode("player_anim")).PlayBackwards("slide_view");
		slide_dir = Vector3.Zero;
		slide_change = Vector3.Zero;

	}
	private Vector3 StartWallRun() 
	{
		//TODO - 	WALL RUNNING
		return vel_stored;
	}
	private void StopWallRun() 
	{
		//TODO - 	WALL RUNNING
		StartFall(vel_stored);
		return;
	}
	private Vector3 StartStun() 
	{
		//TODO - 	STUN STATE
		slow_active = false;
		return vel_stored;
	}
	private void StopStun()
	{
		//TODO - 	STUN STATE
	} 

	/////////////////////////////////////////////////////////////////////////////////////////
	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~Rest of code~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	/////////////////////////////////////////////////////////////////////////////////////////

	private void TestingGettingCollidedObject() {
		//This function is here to test how to grab the object that the player's collision shape is hitting.
		//If this is possible, it would be used in testing if the player has hit jumpable walls or damaging surfaces.
		
		//First you have to grab the number of collisions that have happened since the last MoveAndSlide() call
		int number = GetSlideCollisionCount(); 
		//You grab the all the collider info from the main node (the CharacterBody3D)
		List<StaticBody3D> data = new (number);
		string info_to_display = number.ToString();
		for (int i = 0; i < number; i++) 
		{ 
			data.Add((StaticBody3D)GetSlideCollision(i).GetCollider()); 
			info_to_display += "\n" + data[i].EditorDescription;
		}
		//hud.DebugText(info_to_display);
	}

	public override void _PhysicsProcess(double delta)
    {
		Camera3D cam = (Camera3D)GetNode("head/Camera3D");
		cam.Fov = (float)FOVValue + FOVInfluence;

		hud.ClearDebugText();

		if (!paused)
		{
			if (Input.IsActionJustPressed("jump")) { jump_last_pressed = GetTime(); } //For updating IsJumpJustPressed()
			if (Input.IsActionJustPressed("slide")) { slide_last_pressed = GetTime(); } //For updating IsSlideJustPressed()
		}
		if (IsOnFloor()) { last_on_floor = GetTime(); } //For updating WasOnFloor()
		dodge_recharge_level += WasOnFloor() ? DODGE_GROUNDED_RECHARGE * delta : delta; //Update the dodge cooldown every frame
		if (dodge_recharge_level > DODGE_COOLDOWN_LENGTH) { dodge_recharge_level = DODGE_COOLDOWN_LENGTH; }

		//TODO - When the game is close to release, delete any debug lines AND their associated inputs on the input map
		//Debug cheat code 
		if (Input.IsActionPressed("debug1") && Input.IsActionPressed("debug2") && Input.IsActionPressed("debug3")) 
		{ 
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GetTree().ChangeSceneToFile("res://scenes/debug_menu.tscn"); 
			return;
		} 
		//Fullscreen code (for debugging). This line is kind of terrible and I love it that way
		if (Input.IsActionJustPressed("toggle_fs")) { DisplayServer.WindowSetMode((DisplayServer.WindowMode)((int)GetWindow().Mode == 3 ? 0 : 3)); }
	
		//OOB cancel code
		if (Position.Y < -200) { Position = starting_pos; }
		//Pause input code
		if (Input.IsActionJustPressed("pause")) 
		{ 
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
		vel_stored = velocity_calc(delta * (slow_active ? SLOW_VALUE : 1));
		//GD.Print(vel.Y);
		//hud.DebugText("Vel.Y = " + vel.Y.ToString() + "\nPos.Y = " + Position.Y.ToString() + "\nIsOnFloor() = " + IsOnFloor());
		Velocity = vel_stored * (float)(slow_active ? SLOW_VALUE : 1);
		//hud.AddDebugText("Empty line");
		hud.AddDebugText("WasOnFloor() = " + WasOnFloor());
		hud.AddDebugText(player_state.ToString());
		hud.AddDebugText("y = " + Velocity.Y.ToString());
		hud.AddDebugText("H_vel() = " + H_vel());

		MoveAndSlide();
		//GD.Print("vel = " + vel);
    }

	//Actions code
	private void OnFOVHUDChanged(int value) { FOVValue = value; }
}