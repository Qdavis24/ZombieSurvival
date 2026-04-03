using Godot;
using System.Threading.Tasks;

public partial class PlayerPopup : MarginContainer
{
	[Signal]
	public delegate void PopupFreeEventHandler();
	
	[Export] private Label _text;

	private Vector2 _basePosition = new Vector2(0, 200);

	public void ShowMessage(string message)
	{
		_text.Text = message;
		Visible = true;
	}

	public void HideMessage()
	{
		Visible = false;
	}

	public async Task ShowNotification(string message)
	{
		ShowMessage(message);
		await PlayFadeAnimation();
		HideMessage();
		EmitSignalPopupFree();
	}

	private async Task PlayFadeAnimation()
	{
		_text.Position = _basePosition;
		_text.Modulate = new Color(1, 1, 1, 1);

		var tween = CreateTween();
		tween.TweenProperty(_text, "position:y", _basePosition.Y - 20, .70f);
		tween.Parallel().TweenProperty(_text, "modulate:a", 0f, .70f);

		await ToSignal(tween, Tween.SignalName.Finished);

		_text.Position = _basePosition;
		_text.Modulate = new Color(1, 1, 1, 1);
	}
}
