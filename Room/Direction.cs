using UnityEngine;

public enum Direction
{
    N, S, E, W
}

public static class DirectionExt
{
    public static Vector3 ToVec(this Direction d)
    {
        switch (d)
        {
            case Direction.N: return Vector3.forward;
            case Direction.S: return Vector3.back;
            case Direction.E: return Vector3.right;
            case Direction.W: return Vector3.left;
            default: return Vector3.zero;
        }
    }

    public static Direction Opposite(this Direction d)
    {
        switch (d)
        {
            case Direction.N: return Direction.S;
            case Direction.S: return Direction.N;
            case Direction.E: return Direction.W;
            case Direction.W: return Direction.E;
            default: return d;
        }
    }

    public static Direction[] All => new[] { Direction.N, Direction.S, Direction.E, Direction.W };
}
