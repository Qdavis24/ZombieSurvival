using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerPopup : MarginContainer
{
	[Export] private Label _text;
	
	private string _pickupPrefix = "Press E to pickup the ";
	private string _notificationPrefix = "You picked up the ";

	private Vector2 _basePosition = new Vector2(0, 200);
	
	public void SetPickupText(string itemName)
	{
		_text.Text = _pickupPrefix + itemName;
	}

	public async Task SuccessfullyPickedUp(string itemName)
	{
		_text.Text = _notificationPrefix + itemName;	
		await PlayFadeAnimation();
	}

	private async Task PlayFadeAnimation()
	{
		// Reset state
		_text.Position = _basePosition;
		_text.Modulate = new Color(1, 1, 1, 1);

		var tween = CreateTween();

		// Move up
		tween.TweenProperty(_text, "position:y", _basePosition.Y - 50, 1.0f);

		// Fade out
		tween.Parallel().TweenProperty(_text, "modulate:a", 0f, 1.0f);

		await ToSignal(tween, Tween.SignalName.Finished);

		// Reset after animation
		_text.Position = _basePosition;
		_text.Modulate = new Color(1, 1, 1, 1);
	}
}
