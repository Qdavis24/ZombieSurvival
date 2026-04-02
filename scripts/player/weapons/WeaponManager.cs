using Godot;
using ZombieSurvival.scripts.player.weapons.grenade;

namespace ZombieSurvival.scripts.player.weapons;

public partial class WeaponManager : Node
{
    private sealed class WeaponSlot
    {
        public PackedScene Scene;
        public bool Unlocked;
        public int CurrentAmmo;
        public int ReserveAmmo;

        public WeaponSlot(PackedScene scene, int currentAmmo, int reserveAmmo)
        {
            Scene = scene;
            CurrentAmmo = currentAmmo;
            ReserveAmmo = reserveAmmo;
        }
    }

    [Export] private Node3D _weaponSocket;
    [Export] private PackedScene[] _weaponScenes;
    [Export] private int[] _startingReserveAmmo;
    private WeaponSlot[] _weaponSlots;
    private int _currentWeaponIndex = 0;
    private bool _isSwapping = false;
    private bool _isRocketLoaded = true;

    [Export] private PlayerController _playerController;
    [Export] private Camera _camera;

    [Export] private float _defaultHipFov = 90f;
    [Export] private float _fovLerpSpeed = 80f;

    [Export] private PackedScene _grenadeThrowScene;
    [Export] private int _startingGrenadeCount = 8;
    private int _grenadeCount;
    private bool _isThrowingGrenade = false;
    private int _previousWeaponIndex = -1;

    private HitResolver _hitResolver;
    private UiManager _uiManager;

    private WeaponBase _current;

    public override void _Ready()
    {
        _uiManager = GetTree().CurrentScene.GetNodeOrNull<UiManager>("UiManager");
        _hitResolver = GetTree().CurrentScene.GetNodeOrNull<HitResolver>("HitResolver");
        
        _grenadeCount = _startingGrenadeCount;

        if (_weaponScenes != null && _weaponScenes.Length > 0)
        {
            _weaponSlots = new WeaponSlot[_weaponScenes.Length];

            for (int i = 0; i < _weaponScenes.Length; i++)
            {
                var scene = _weaponScenes[i];
                if (scene == null)
                    continue;

                var previewWeapon = scene.Instantiate<WeaponBase>();
                int magazineSize = previewWeapon.MagazineSize;
                int reserveAmmo = (_startingReserveAmmo != null && i < _startingReserveAmmo.Length)
                    ? _startingReserveAmmo[i]
                    : 0;

                _weaponSlots[i] = new WeaponSlot(scene, magazineSize, reserveAmmo)
                {
                    // Unlocked = i == 0 // final version just pistol unlocked
                    Unlocked = i == 0 || i == 1 || i == 2 || i == 3
                };

                previewWeapon.QueueFree();
            }

            _currentWeaponIndex = 0;
            Equip(_currentWeaponIndex);
        }
    }

    private void Equip(int weaponIndex)
    {
        if (_weaponSlots == null || weaponIndex < 0 || weaponIndex >= _weaponSlots.Length)
            return;

        var slot = _weaponSlots[weaponIndex];
        if (slot == null || slot.Scene == null)
            return;

        if (_current != null)
        {
            _current.Fired -= _camera.OnWeaponFired;
            _current.Fired -= _playerController.OnWeaponFired;
            _current.AmmoChanged -= OnCurrentWeaponAmmoChanged;
        }

        _current?.QueueFree();

        _current = slot.Scene.Instantiate<WeaponBase>();
        if (_current is RpgWeapon rpg)
        {
            rpg.SetRocketLoaded(_isRocketLoaded);
        }
        _weaponSocket.AddChild(_current);
        _current.Initialize(_camera, _hitResolver, slot.CurrentAmmo, slot.ReserveAmmo);
        _current.Fired += _camera.OnWeaponFired;
        _current.Fired += _playerController.OnWeaponFired;
        _current.AmmoChanged += OnCurrentWeaponAmmoChanged;

        RefreshHudAmmo();
        CallDeferred(nameof(RefreshHudAmmo));
    }

    private void OnCurrentWeaponAmmoChanged(int currentAmmo, int reserveAmmo)
    {
        if (_weaponSlots != null && _currentWeaponIndex >= 0 && _currentWeaponIndex < _weaponSlots.Length)
        {
            var slot = _weaponSlots[_currentWeaponIndex];
            if (slot != null)
            {

                if (_current is RpgWeapon)
                {
                    // Update rocket loaded state
                    _isRocketLoaded = currentAmmo != 0;
                }
                
                slot.CurrentAmmo = currentAmmo;
                slot.ReserveAmmo = reserveAmmo;
            }
        }

        _uiManager.HudSetAmmo(_current.CurrentAmmo, _current.ReserveAmmo);
    }

    private void RefreshHudAmmo()
    {
        if (_uiManager == null || _current == null)
            return;

        _uiManager.HudSetAmmo(_current.CurrentAmmo, _current.ReserveAmmo);
    }

    private AnimationPlayer GetWeaponAnimationPlayer(WeaponBase weapon)
    {
        return weapon?.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
    }

    private async void TryStartGrenadeThrow()
    {
        if (_isSwapping || _isThrowingGrenade)
            return;

        if (_grenadeCount <= 0)
            return;

        if (_grenadeThrowScene == null)
            return;

        _isThrowingGrenade = true;
        _grenadeCount--;
        _previousWeaponIndex = _currentWeaponIndex;

        if (_current != null)
        {
            var currentAnim = GetWeaponAnimationPlayer(_current);
            if (currentAnim != null && currentAnim.HasAnimation("transition_swap"))
            {
                _current.SetAimState(false);
                currentAnim.Play("transition_swap");
                await ToSignal(currentAnim, AnimationPlayer.SignalName.AnimationFinished);
            }

            _current.Fired -= _camera.OnWeaponFired;
            _current.Fired -= _playerController.OnWeaponFired;
            _current.AmmoChanged -= OnCurrentWeaponAmmoChanged;
            _current.QueueFree();
            _current = null;
        }

        var grenadeThrowNode = _grenadeThrowScene.Instantiate<Grenade>();
        _weaponSocket.AddChild(grenadeThrowNode);

        await grenadeThrowNode.ThrowGrenade();

        grenadeThrowNode.QueueFree();

        Equip(_previousWeaponIndex);

        if (_current != null)
        {
            var currentAnim = GetWeaponAnimationPlayer(_current);
            if (currentAnim != null && currentAnim.HasAnimation("transition_swap"))
            {
                currentAnim.Play("transition_swap");
                currentAnim.Seek(currentAnim.CurrentAnimationLength, true);
                currentAnim.Play("transition_swap", customSpeed: -1.0f, fromEnd: true);
                await ToSignal(currentAnim, AnimationPlayer.SignalName.AnimationFinished);
            }
        }

        _isThrowingGrenade = false;
    }
    
    private async void SwapToWeaponIndex(int newIndex)
    {
        if (_isSwapping) return;
        if (_weaponSlots == null || _weaponSlots.Length == 0) return;
        if (newIndex < 0 || newIndex >= _weaponSlots.Length) return;
        if (_weaponSlots[newIndex] == null || !_weaponSlots[newIndex].Unlocked) return;
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
        Equip(_currentWeaponIndex);

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
        if (_weaponSlots == null || _weaponSlots.Length == 0)
            return -1;

        int index = _currentWeaponIndex;
        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            index += direction;
            if (index >= _weaponSlots.Length)
                index = 0;
            else if (index < 0)
                index = _weaponSlots.Length - 1;

            if (_weaponSlots[index] != null && _weaponSlots[index].Unlocked)
                return index;
        }

        return _currentWeaponIndex;
    }

    public override void _Process(double delta)
    {
        // Swap weapon
        if (!_isSwapping && _weaponSlots != null && _weaponSlots.Length > 0)
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
            if (Input.IsActionJustPressed("weapon1"))
                SwapToWeaponIndex(0);
            if (Input.IsActionJustPressed("weapon2"))
                SwapToWeaponIndex(1);
            if (Input.IsActionJustPressed("weapon3"))
                SwapToWeaponIndex(2);
            if (Input.IsActionJustPressed("weapon4"))
                SwapToWeaponIndex(3);
            
            if (Input.IsActionJustPressed("throw_grenade"))
                TryStartGrenadeThrow();

            if (_isThrowingGrenade)
                return;

            bool aimHeld = Input.IsActionPressed("aim");
            _current?.SetAimState(aimHeld);
            float targetFov = _current != null ? _current.GetTargetFov() : _defaultHipFov;
            _camera.Fov = Mathf.MoveToward(_camera.Fov, targetFov, (float)(_fovLerpSpeed * delta));

            bool isMovingForward = Input.IsActionPressed("move_forward");
            _current?.SetMovementState(isMovingForward);
            _camera.SetMovementState(isMovingForward);

            if (Input.IsActionJustPressed("reload"))
                _current?.Call("TryReload");

            bool triggerHeld = Input.IsActionPressed("fire");
            _current?.TryFire(triggerHeld);
        }
    }
}