using Godot;
using System;

public partial class Containers : Node
{
	public static Containers Instance { get; private set; }
    
	public Node VFX { get; private set; }
	public Node Limbs { get; private set; }
	public Node Projectiles { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		VFX = GetNode("Vfx");
		Limbs = GetNode("Limbs");
		Projectiles = GetNode("Projectiles");
	}
}
