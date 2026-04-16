using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class AmmoStore : Node3D
{
    [Export] private InteractNotifier _interactNotifier;
    [Export] private PackedScene _ammoScene;
    [Export] private int _storeSellAmount;
    [Export] private ItemType _storeBuyItemType;
    [Export] private int _storeBuyAmount;
    [Export] private Timer _cooldown;

    [Export] private AudioStream _failedBuySound;
    [Export] private AudioStream _successfulBuySound;
    [Export] private Marker3D _marker;
    
    [ExportCategory("Text")]
    [Export] private string _storeType;
    [Export] private MeshInstance3D _textMesh;

    private bool _ready = true;

    private int _totalAmmoUponBuying = 0;


    public override void _Ready()
    {
        _cooldown.Timeout += CooldownOnTimeout;
        _interactNotifier.PlayerEnteredRange += OnPlayerEnteredRange;
        _interactNotifier.PlayerExitedRange += OnPlayerExitedRange;
        _interactNotifier.Interacted += OnInteracted;

        _interactNotifier.InteractPrompt = $"press e to spend {_storeBuyAmount} points for {_storeType}";
        InitShopText();
    }

    private void CooldownOnTimeout()
    {
        _ready = true;
    }

    private void OnPlayerEnteredRange()
    {
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerEnteredInteractableRange, _interactNotifier);
    }

    private void OnPlayerExitedRange()
    {
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerExitedInteractableRange, _interactNotifier);
    }

    private void OnInteracted(Node3D player)
    {
        if (!_ready) return;
        if (player is not IInventoryOwner inventoryOwner) return;
        if (inventoryOwner.Inventory.ConsumeItem(_storeBuyItemType, _storeBuyAmount))
        {
            _ready = false;
            _cooldown.Start();
            AudioManager.I.Play3D(_successfulBuySound, GlobalPosition);
            for (int i = 0; i < _storeSellAmount; i++)
                SpawnAmmo();
      
        }
        else
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
    }

    private void SpawnAmmo()
    {
        var ammo = _ammoScene.Instantiate<Pickup>();
        Containers.Instance.VFX.AddChild(ammo);
        ammo.GlobalPosition = _marker.GlobalPosition;
        ammo.ApplyImpulse(_marker.Basis.Z * 1f);
    }

    private void InitShopText()
    {
        if (_textMesh == null || _textMesh.Mesh == null)
            return;

        _textMesh.Mesh = _textMesh.Mesh.Duplicate() as Mesh;

        if (_textMesh.Mesh is not TextMesh textMesh)
            return;

        // Calculate total ammo preview
        int ammoPerPickup = 0;
        var tempInstance = _ammoScene.Instantiate<Pickup>();
        if (tempInstance != null)
        {
            ammoPerPickup = tempInstance.GetItemAmount();
            tempInstance.QueueFree();
        }

        _totalAmmoUponBuying = ammoPerPickup * _storeSellAmount;

        // textMesh.Text = $"{_totalAmmoUponBuying} {_storeType}\nfor {_storeBuyAmount}\nParasitic Material";
        textMesh.FontSize = 30;
        textMesh.Text = $"{_storeType}";
    }
}