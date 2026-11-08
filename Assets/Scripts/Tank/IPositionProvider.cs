using UnityEngine;

namespace Tank
{
    public interface IPositionProvider
    {
        Vector3 Position { get; }
    }
}
