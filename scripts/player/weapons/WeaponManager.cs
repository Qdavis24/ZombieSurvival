using Godot;

public partial class WeaponManager : Node
{
    [Export] private NodePath _playerControllerPath;
    [Export] private NodePath _weaponSocketPath;
    [Export] private PackedScene[] _weapons; // All possible weapons
    private bool[] _weaponUnlocked; // Tracks which weapons the player has unlocked
    private int _currentWeaponIndex = 0;
    private bool _isSwapping = false;
    
    [Export] private float _defaultHipFov = 90f;
    [Export] private float _fovLerpSpeed = 80f;
    
    private const string HitResolverPath = "../../HitResolver";

    private PlayerController _playerController;
    private Node3D _weaponSocket;
    private WeaponBase _current;
    private Camera _camera;
    private HitResolver _hitResolver;

    public override void _Ready()
    {
        _weaponSocket = GetNode<Node3D>(_weaponSocketPath);
        _playerController = GetNode<PlayerController>(_playerControllerPath);

        _camera = GetParent()
            .GetNode<Node3D>("Head")
            .GetNode<Camera>("Camera3D");

        _hitResolver = GetNode<HitResolver>(HitResolverPath);

        // Initialize unlock state for weapons
        if (_weapons != null)
        {
            _weaponUnlocked = new bool[_weapons.Length];
            if (_weaponUnlocked.Length > 0)
                _weaponUnlocked[0] = true; // First weapon unlocked by default
        }
        _weaponUnlocked[1] = true; // First weapon unlocked by default

        if (_weapons != null && _weapons.Length > 0)
        {
            _currentWeaponIndex = 0;
            Equip(_weapons[_currentWeaponIndex]);
        }
    }

    private void Equip(PackedScene weaponScene)
    {
        if (_current != null)
        {
            _current.Fired -= _camera.OnWeaponFired;
            _current.Fired -= _playerController.OnWeaponFired;
        }

        _current?.QueueFree();

        _current = weaponScene.Instantiate<WeaponBase>();
        _weaponSocket.AddChild(_current);
        _current.Initialize(_camera, _hitResolver);
        _current.Fired += _camera.OnWeaponFired; // Listen to shots for recoil
        _current.Fired += _playerController.OnWeaponFired; // Listen to shots for recoil
    }

    private AnimationPlayer GetWeaponAnimationPlayer(WeaponBase weapon)
    {
        return weapon?.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
    }

    private async void SwapToWeaponIndex(int newIndex)
    {
        if (_isSwapping) return;
        if (_weapons == null || _weapons.Length == 0) return;
        if (newIndex < 0 || newIndex >= _weapons.Length) return;
        if (!_weaponUnlocked[newIndex]) return;
        if (newIndex == _currentWeaponIndex) return;

        _isSwapping = true;

        if (_current != null)
        {
            var currentAnim = GetWeaponAnimationPlayer(_current);
            if (currentAnim != null && currentAnim.HasAnimation("transition_swap"))
            {
                _current.SetAimState(false);
                // Stow animation
                currentAnim.Play("transition_swap");
                await ToSignal(currentAnim, AnimationPlayer.SignalName.AnimationFinished);
            }
        }

        _currentWeaponIndex = newIndex;
        Equip(_weapons[_currentWeaponIndex]);

        if (_current != null)
        {
            var newAnim = GetWeaponAnimationPlayer(_current);
            if (newAnim != null && newAnim.HasAnimation("transition_swap"))
            {
                // Force the weapon into the stowed pose immediately so it doesn't flash idle
                newAnim.Play("transition_swap");
                newAnim.Seek(newAnim.CurrentAnimationLength, true);

                // Animation of equip (playing stow animation backwards)
                newAnim.Play("transition_swap", customSpeed: -1.0f, fromEnd: true);
                await ToSignal(newAnim, AnimationPlayer.SignalName.AnimationFinished);
            }
        }

        _isSwapping = false;
    }

    private int FindNextUnlockedWeaponIndex(int direction)
    {
        if (_weapons == null || _weapons.Length == 0)
            return -1;

        int index = _currentWeaponIndex;
        for (int i = 0; i < _weapons.Length; i++)
        {
            index += direction;
            if (index >= _weapons.Length)
                index = 0;
            else if (index < 0)
                index = _weapons.Length - 1;

            if (_weaponUnlocked[index])
                return index;
        }

        return _currentWeaponIndex;
    }

    public override void _Process(double delta)
    {
        // Swap weapon
        if (!_isSwapping && _weapons != null && _weapons.Length > 0)
        {
            if (Input.IsActionJustPressed("weapon_swap_down"))
            {
                int nextIndex = FindNextUnlockedWeaponIndex(1);
                SwapToWeaponIndex(nextIndex);
            }

            if (Input.IsActionJustPressed("weapon_swap_up"))
            {
                int nextIndex = FindNextUnlockedWeaponIndex(-1);
                SwapToWeaponIndex(nextIndex);
            }
        }

        if (!_isSwapping)
        {
            bool aimHeld = Input.IsActionPressed("aim");
            _current?.SetAimState(aimHeld);
            float targetFov = _current != null ? _current.GetTargetFov() : _defaultHipFov;
            _camera.Fov = Mathf.MoveToward(_camera.Fov, targetFov, (float)(_fovLerpSpeed * delta));

            bool isMovingForward = Input.IsActionPressed("move_forward");
            _current?.SetMovementState(isMovingForward);
            _camera.SetMovementState(isMovingForward);
        
            bool triggerHeld = Input.IsActionPressed("fire");
            _current?.TryFire(triggerHeld);
        }
    }
}