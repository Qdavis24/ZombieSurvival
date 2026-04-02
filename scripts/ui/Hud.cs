using Godot;

public partial class Hud : CanvasLayer
{
    [ExportGroup("Nodes")]
    [Export] private Label _round;

    private int[] _values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
    private string[] _numerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

    public void SetRound(int round)
    {
        _round.Text = IntToRoman(round);
    }

    private string IntToRoman(int num)
    {
        var result = "";

        for (int i = 0; i < _values.Length; i++)
        {
            while (num >= _values[i])
            {
                result += _numerals[i];
                num -= _values[i];
            }
        }

        return result;
    }
}
