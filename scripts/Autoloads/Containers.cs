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

	// Frees everything spawned during a run so it doesn't carry over into the next one.
	public void Clear()
	{
		foreach (var container in new[] { VFX, Limbs, Projectiles })
			foreach (var child in container.GetChildren())
				child.QueueFree();
	}
}
