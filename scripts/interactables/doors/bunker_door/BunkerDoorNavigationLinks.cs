using Godot;

public partial class BunkerDoorNavigationLinks : Node
{
    [Export] private BunkerDoor _rightSideDoor;
    [Export] private NavigationLink3D _rightSideLink;
    [Export] private NavigationLink3D _rightSideLink2;
    [Export] private BunkerDoor _leftSideDoor;
    [Export] private NavigationLink3D _leftSideLink;
    [Export] private NavigationLink3D _leftSideLink2;
    [Export] private BunkerDoor _blastDoor;
    [Export] private NavigationLink3D _blastDoorLink;

    public override void _Ready()
    {
        _rightSideDoor.Opened += () =>
        {
            _rightSideLink.Enabled = true;
            _rightSideLink2.Enabled = true;
        };
        _leftSideDoor.Opened += () =>
        {
            _leftSideLink.Enabled = true;
            _leftSideLink2.Enabled = true;
        };
        _blastDoor.Opened += () => _blastDoorLink.Enabled = true;
    }
}
