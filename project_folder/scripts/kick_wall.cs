using Godot;
using System;

[Tool]
public partial class kick_wall : StaticBody3D
{
	[Export]
	private Vector3 wall_size = Vector3.One;

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) {
			CollisionShape3D hitbox = (CollisionShape3D)GetNode("CollisionShape3D");
			((BoxShape3D)hitbox.Shape).Size = wall_size;
			MeshInstance3D mesh = (MeshInstance3D)GetNode("MeshInstance3D");
			((BoxMesh)mesh.Mesh).Size = wall_size;
		}
    }
}
