using System;
using System.Threading.Tasks;
using Godot;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.player.weapons.grenade;

namespace ZombieSurvival.scripts.player.weapons;

public partial class WeaponManager : Node
{
    [Signal]
    public delegate void AmmoChangedEventHandler(int currentAmmo, int reserve);

    [Signal]
    public delegate void GrenadesChangedEventHandler(int currentGrenades);

    [Signal]
    public delegate void ReloadFailedEventHandler();
    
    [Signal]
    public delegate void GrenadeThrowFailedEventHandler();

    private sealed class WeaponSlot
    {
        public PackedScene Scene;
        public bool Unlocked;
        public int CurrentAmmo;
        public ItemType AmmoType;
        public ItemType WeaponType;

        public WeaponSlot(PackedScene scene, int currentAmmo, ItemType ammoType, ItemType weaponType)
        {
            Scene = scene;
            CurrentAmmo = currentAmmo;
            AmmoType = ammoType;
            WeaponType = weaponType;
        }
    }

    private enum HandActionState
    {
        Idle,
        SwappingWeapon,
        ThrowingGrenade
    }

    [Export] private Node3D _weaponSocket;
    [Export] private PackedScene[] _weaponScenes;
    [Export] private int[] _startingReserveAmmo;
    private WeaponSlot[] _weaponSlots;
    private int _currentWeaponIndex = 0;
    private bool _isRocketLoaded = true;
    private bool _crosshairHiddenByWeaponState = false;
    private HandActionState _handActionState = HandActionState.Idle;

    [Export] private PlayerController _playerController;
    [Export] private Camera _camera;

    [Export] private float _defaultHipFov = 90f;
    [Export] private float _fovLerpSpeed = 80f;

    [Export] private PackedScene _grenadeThrowScene;
    [Export] private int _startingGrenadeCount = 8;

    private HitResolver _hitResolver;

    private WeaponBase _current;
    private bool IsHandsBusy => _handActionState != HandActionState.Idle;

    public override void _Ready()
    {
        _hitResolver = GetTree().CurrentScene.GetNodeOrNull<HitResolver>("HitResolver");
        _playerController.Inventory.ItemAdded += OnPlayerInventoryItemAdded;
        _playerController.Inventory.ItemRemoved += OnPlayerInventoryItemRemoved;
        _playerController.Inventory.AddItem(ItemType.Grenades, _startingGrenadeCount);
        
        CallDeferred(nameof(RefreshHudGrenades));

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
                ItemType ammoType = previewWeapon.AmmoType;
                ItemType weaponType = previewWeapon.WeaponType;

                _weaponSlots[i] = new WeaponSlot(scene, magazineSize, ammoType, weaponType)
                {
                    // Unlocked = i == 0 // final version just pistol unlocked
                    Unlocked = i == 0
                };

                _playerController.Inventory.AddItem(ammoType, reserveAmmo);

                previewWeapon.QueueFree();
            }

            _currentWeaponIndex = 0;
            Equip(_currentWeaponIndex);
        }
    }

    private void OnPlayerInventoryItemAdded(ItemType itemType, int amount)
    {
        if (itemType == ItemType.Grenades)
        {
            RefreshHudGrenades();
            return;
        }

        if (_weaponSlots == null) return;
        switch (itemType.GetGroup())
        {
            case (ItemGroup.Ammo):
                if (_weaponSlots[_currentWeaponIndex].AmmoType == itemType)
                    RefreshHudAmmo();
                break;
            case (ItemGroup.Weapon):
                for (int i = 0; i < _weaponSlots.Length; i++)
                {
                    if (_weaponSlots[i].WeaponType == itemType)
                        _weaponSlots[i].Unlocked = true;
                }

                break;
        }
    }

    private void OnPlayerInventoryItemRemoved(ItemType itemType, int amount)
    {
        if (itemType == ItemType.Grenades)
        {
            RefreshHudGrenades();
            return;
        }

        if (_weaponSlots == null) return;
        if (_weaponSlots[_currentWeaponIndex].AmmoType == itemType)
            RefreshHudAmmo();
    }

    private void RefreshHudGrenades()
    {
        var count = _playerController.Inventory.GetAmount(ItemType.Grenades);
        EmitSignal(SignalName.GrenadesChanged, count);
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
            _current.ReloadFailed -= OnCurrentWeaponReloadFailed;
        }

        _current?.QueueFree();

        _current = slot.Scene.Instantiate<WeaponBase>();
        if (_current is RpgWeapon rpg)
        {
            rpg.SetRocketLoaded(_isRocketLoaded);
        }

        _weaponSocket.AddChild(_current);
        _current.Initialize(_camera, _hitResolver, slot.CurrentAmmo);
        _current.SetAmmoSource(
            needed =>
            {
                var available = _playerController.Inventory.GetAmount(slot.AmmoType);
                var granted = Math.Min(needed, available);
                if (granted > 0)
                    _playerController.Inventory.ConsumeItem(slot.AmmoType, granted);
                return granted;
            },
            () => _playerController.Inventory.GetAmount(slot.AmmoType)
        );        
        _current.Fired += _camera.OnWeaponFired;
        _current.Fired += _playerController.OnWeaponFired;
        _current.AmmoChanged += OnCurrentWeaponAmmoChanged;
        _current.ReloadFailed += OnCurrentWeaponReloadFailed;

        RefreshHudAmmo();
        CallDeferred(nameof(RefreshHudAmmo));
    }

    private void OnCurrentWeaponAmmoChanged(int currentAmmo)
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
            }
        }

        RefreshHudAmmo();
    }

    private void OnCurrentWeaponReloadFailed()
    {
        EmitSignal(SignalName.ReloadFailed);
    }

    private void RefreshHudAmmo()
    {
        if (_current == null || _weaponSlots == null)
            return;

        var reserve = _playerController.Inventory.GetAmount(_weaponSlots[_currentWeaponIndex].AmmoType);
        EmitSignal(SignalName.AmmoChanged, _current.CurrentAmmo, reserve);
    }


    private void UpdateCrosshairVisibility(bool isMovingForward)
    {
        bool shouldHideCrosshair = isMovingForward || IsHandsBusy || (_current?.IsReloading ?? false) || (_current?.IsAiming ?? false);

        if (shouldHideCrosshair)
        {
            if (!_crosshairHiddenByWeaponState)
            {
                _playerController.HideCrosshair();
                _crosshairHiddenByWeaponState = true;
            }
        }
        else if (_crosshairHiddenByWeaponState)
        {
            _playerController.ShowCrosshair();
            _crosshairHiddenByWeaponState = false;
        }
    }

    private AnimationPlayer GetWeaponAnimationPlayer(WeaponBase weapon)
    {
        return weapon?.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
    }

    private void DisconnectCurrentWeapon()
    {
        if (_current == null)
            return;

        _current.Fired -= _camera.OnWeaponFired;
        _current.Fired -= _playerController.OnWeaponFired;
        _current.AmmoChanged -= OnCurrentWeaponAmmoChanged;
        _current.ReloadFailed -= OnCurrentWeaponReloadFailed;
    }

    private async Task StowCurrentWeapon()
    {
        if (_current == null)
            return;

        var currentAnim = GetWeaponAnimationPlayer(_current);
        if (currentAnim != null && currentAnim.HasAnimation("transition_swap"))
        {
            _current.SetAimState(false);
            currentAnim.Play("transition_swap");
            await ToSignal(currentAnim, AnimationPlayer.SignalName.AnimationFinished);
        }

        DisconnectCurrentWeapon();
        _current.QueueFree();
        _current = null;
    }

    private async Task RaiseCurrentWeapon()
    {
        if (_current == null)
            return;

        var currentAnim = GetWeaponAnimationPlayer(_current);
        if (currentAnim != null && currentAnim.HasAnimation("transition_swap"))
        {
            currentAnim.Play("transition_swap");
            currentAnim.Seek(currentAnim.CurrentAnimationLength, true);
            currentAnim.Play("transition_swap", customSpeed: -1.0f, fromEnd: true);
            await ToSignal(currentAnim, AnimationPlayer.SignalName.AnimationFinished);
        }
    }

    private async void TryStartGrenadeThrow()
    {
        if (IsHandsBusy)
            return;

        if (_playerController.Inventory.GetAmount(ItemType.Grenades) <= 0)
        {
            EmitSignal(SignalName.GrenadeThrowFailed);
            return;
        }

        if (_grenadeThrowScene == null)
            return;

        _handActionState = HandActionState.ThrowingGrenade;

        try
        {
            _playerController.Inventory.ConsumeItem(ItemType.Grenades, 1);
            int previousWeaponIndex = _currentWeaponIndex;

            await StowCurrentWeapon();

            var grenadeThrowNode = _grenadeThrowScene.Instantiate<Grenade>();
            _weaponSocket.AddChild(grenadeThrowNode);

            await grenadeThrowNode.ThrowGrenade();

            grenadeThrowNode.QueueFree();

            Equip(previousWeaponIndex);
            await RaiseCurrentWeapon();
        }
        finally
        {
            _handActionState = HandActionState.Idle;
        }
    }

    private async void SwapToWeaponIndex(int newIndex)
    {
        if (IsHandsBusy) return;
        if (_weaponSlots == null || _weaponSlots.Length == 0) return;
        if (newIndex < 0 || newIndex >= _weaponSlots.Length) return;
        if (_weaponSlots[newIndex] == null || !_weaponSlots[newIndex].Unlocked) return;
        if (newIndex == _currentWeaponIndex) return;

        _handActionState = HandActionState.SwappingWeapon;

        try
        {
            await StowCurrentWeapon();

            _currentWeaponIndex = newIndex;
            Equip(_currentWeaponIndex);
            await RaiseCurrentWeapon();
        }
        finally
        {
            _handActionState = HandActionState.Idle;
        }

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
        bool isMovingForward = Input.IsActionPressed("move_forward");
        UpdateCrosshairVisibility(isMovingForward);

        // Swap weapon
        if (!IsHandsBusy && _weaponSlots != null && _weaponSlots.Length > 0)
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

        if (!IsHandsBusy)
        {
            if (Input.IsActionJustPressed("weapon1"))
                SwapToWeaponIndex(0);
            if (Input.IsActionJustPressed("weapon2"))
                SwapToWeaponIndex(1);
            if (Input.IsActionJustPressed("weapon3"))
                SwapToWeaponIndex(2);
            if (Input.IsActionJustPressed("weapon4"))
                SwapToWeaponIndex(3);

            if (IsHandsBusy)
                return;

            if (Input.IsActionJustPressed("throw_grenade"))
                TryStartGrenadeThrow();

            if (IsHandsBusy)
                return;

            bool aimHeld = Input.IsActionPressed("aim");
            _current?.SetAimState(aimHeld);
            float targetFov = _current != null ? _current.GetTargetFov() : _defaultHipFov;
            _camera.Fov = Mathf.MoveToward(_camera.Fov, targetFov, (float)(_fovLerpSpeed * delta));

            _current?.SetMovementState(isMovingForward);
            _camera.SetMovementState(isMovingForward);

            if (Input.IsActionJustPressed("reload"))
            {
                var slot = _weaponSlots?[_currentWeaponIndex];
                if (_current != null && slot != null)
                    _current.TryReload();
            }

            bool triggerHeld = Input.IsActionPressed("fire");
            _current?.TryFire(triggerHeld);
        }
    }
}
