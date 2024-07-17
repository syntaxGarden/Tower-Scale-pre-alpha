using Godot;
using System;

//The following code is adapted/stolen from the video "Godot 4 Projectiles Tutorial" by Gwiss (link: https://youtu.be/YPvPqOqbx-I)

public partial class Bullet : CharacterBody3D
{
	public double EndTime;
	const double BULLET_LIFETIME = 3;
	//private Vector3 dir;
	private Vector3 spawnPos; 
	private Vector3 spawnDir;
	[Export]
	private double SPEED = 80;
	private static double GetTime() { return DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds; }
    public override void _Ready()
    {
        GlobalPosition = spawnPos;
		GlobalRotation = spawnDir;
    }
	public void SetBulletValues(Vector3 new_dir, Vector3 new_spawnPos, double start_time)
	{
		//dir = new_dir;
		spawnPos = new_spawnPos;
		spawnDir = new_dir;
		Velocity = Vector3.Forward.Rotated(Vector3.Right, spawnDir.X).Rotated(Vector3.Up, spawnDir.Y) * (float)SPEED;
		EndTime = start_time + BULLET_LIFETIME;
	}
	public void RemoveBullet() { QueueFree(); } //This function is to be used when the bullet has interacted with an object and that object processes the collision data and then removes the bullet
    public override void _PhysicsProcess(double delta)
    {
		if (EndTime > GetTime()) { MoveAndCollide(Velocity * (float)delta); } else { RemoveBullet(); }
    }
}	
