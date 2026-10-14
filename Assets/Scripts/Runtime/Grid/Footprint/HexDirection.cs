/// <summary>
/// The six neighbour directions of a pointy-top hex, in grid space (not world or camera space), listed
/// counter-clockwise starting from East. Values are explicit and serialized: never renumber them.
/// </summary>
public enum HexDirection
{
    East = 0,
    NorthEast = 1,
    NorthWest = 2,
    West = 3,
    SouthWest = 4,
    SouthEast = 5,
}
