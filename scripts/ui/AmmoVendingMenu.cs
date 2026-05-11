using System.Collections.Generic;
using Godot;

public partial class AmmoVendingMenu : CanvasLayer
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

    public void ShowOptions(IReadOnlyList<AmmoVendingOption> options)
    {
        ClearOptionButtons();

        if (_titleLabel != null)
            _titleLabel.Text = "Ammo Vending Machine";

        if (_optionsContainer != null && options != null)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option == null)
                    continue;

                var optionIndex = i;
                var button = new Button
                {
                    Text = $"{option.DisplayName} - {option.Price}",
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

    private void ClearOptionButtons()
    {
        foreach (var button in _optionButtons)
            button.QueueFree();

        _optionButtons.Clear();
    }
}
