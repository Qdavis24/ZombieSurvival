using Godot;

public partial class ControlsMenu : CanvasLayer
{
	[Signal] public delegate void ClosedEventHandler();

	[Export] private Button _closeButton;

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

	public void Open()
	{
		Visible = true;
	}

	public void Close()
	{
		if (!Visible)
			return;

		Visible = false;
		EmitSignal(SignalName.Closed);
	}
}
