using Godot;
using System.Threading.Tasks;
using System;

public partial class Grenade : Node3D
{
	[Export] private PackedScene _grenadeProjectileScene;
	[Export] private Marker3D _throwSpawnPoint;
	[Export] private AnimationPlayer _animPlayer;
	[Export] private MeshInstance3D _grenadeAnimMesh;
	[Export] private MeshInstance3D _grenadeRingAnimMesh;
	[Export] private float _grenadeThrowMomentDelay = 0.5f;
	
	public async Task ThrowGrenade()
	{
		_animPlayer.Play("grenade_throw");
		await ToSignal(GetTree().CreateTimer(_grenadeThrowMomentDelay), SceneTreeTimer.SignalName.Timeout);
		_grenadeAnimMesh.Visible = false;
		_grenadeRingAnimMesh.Visible = false;

		SpawnThrownGrenade();
		
		await ToSignal(_animPlayer, AnimationPlayer.SignalName.AnimationFinished);
	}

	private void SpawnThrownGrenade()
	{
		var grenade = _grenadeProjectileScene.Instantiate<GrenadeProjectile>();
		GetTree().CurrentScene.AddChild(grenade);
		
		grenade.GlobalTransform = _throwSpawnPoint.GlobalTransform;

		Vector3 throwDirection = -_throwSpawnPoint.GlobalTransform.Basis.Z;
		grenade.Throw(throwDirection);
	}
}
