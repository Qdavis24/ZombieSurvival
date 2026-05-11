using System.Collections.Generic;
using Godot;

public partial class PerkVendingMenu : CanvasLayer
{
    [Signal] public delegate void OptionPressedEventHandler(int optionIndex);
    [Signal] public delegate void ClosedEventHandler();

    [Export] private Label _titleLabel;
    [Export] private VBoxContainer _optionsContainer;
    [Export] private Button _closeButton;

    private readonly List<Button> _optionButtons = new();

    public override void _Ready()
    {
        Visible = false;

        if (_closeButton != null)
            _closeButton.Pressed += Close;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
            return;

        if (@event.IsActionPressed("pause"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    public void ShowOptions(PerkVendingMachine machine, Node3D player)
    {
        ClearOptionButtons();

        if (_titleLabel != null)
            _titleLabel.Text = "Perk Vending Machine";

        var options = machine?.Options;
        if (_optionsContainer != null && options != null)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option == null)
                    continue;

                var optionIndex = i;
                var status = machine.GetPurchaseStatus(optionIndex, player);
                var button = new Button
                {
                    Text = GetButtonText(option, status),
                    Disabled = status != PerkVendingMachine.PurchaseStatus.Available,
                    CustomMinimumSize = new Vector2(360f, 44f),
                    FocusMode = Control.FocusModeEnum.All
                };

                button.Pressed += () => EmitSignal(SignalName.OptionPressed, optionIndex);
                _optionsContainer.AddChild(button);
                _optionButtons.Add(button);
            }
        }

        Visible = true;
    }

    public void Close()
    {
        if (!Visible)
            return;

        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    private static string GetButtonText(PerkVendingOption option, PerkVendingMachine.PurchaseStatus status)
    {
        return status switch
        {
            PerkVendingMachine.PurchaseStatus.Owned => $"{option.DisplayName} - Owned",
            PerkVendingMachine.PurchaseStatus.NotEnoughPoints => $"{option.DisplayName} - {option.Price}",
            PerkVendingMachine.PurchaseStatus.Busy => $"{option.DisplayName} - Unavailable",
            PerkVendingMachine.PurchaseStatus.Invalid => $"{option.DisplayName} - Unavailable",
            _ => $"{option.DisplayName} - {option.Price}"
        };
    }

    private void ClearOptionButtons()
    {
        foreach (var button in _optionButtons)
            button.QueueFree();

        _optionButtons.Clear();
    }
}
