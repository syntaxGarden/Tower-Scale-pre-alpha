using Godot;
using System;
using System.Collections.Generic;

public partial class test_area : Node3D
{
    Player p;
    PackedScene bullet;
    private static double GetTime() { return DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds; }
    public override void _Ready()
    {
        p = (Player)GetNode("Player");
        p.UnlockAllArms();
        p.ShotFired += FirePlayerBullet;
        bullet = ResourceLoader.Load<PackedScene>("res://scenes/Bullet.tscn");
    }
    // Called when the node enters the scene tree for the first time.
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("game_insta_end")) { GetTree().Quit(); }
        if (Input.IsActionJustPressed("arm")) 
        { 
            
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        //
    }

    public void FirePlayerBullet()
    {
        //To instance a bullet:
        //1. var inst = scene.instance()
        //2. Affect scene variables as you wish
        //3. AddChild(inst)
        //Optional: you can create a bullet list to keep track of all of the bullet instances if need be
        Bullet inst = bullet.Instantiate<Bullet>();
        Node3D h = (Node3D)p.GetNode("head");
        Vector3 bullet_dir = new Vector3(h.Rotation.X, p.Rotation.Y, 0);
        Vector3 bullet_pos = p.Position + Vector3.Up;
        inst.SetBulletValues(bullet_dir, bullet_pos, GetTime());
         AddChild(inst);
    }
}
