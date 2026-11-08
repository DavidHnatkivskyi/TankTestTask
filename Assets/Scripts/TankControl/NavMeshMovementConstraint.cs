using Tank;
using UnityEngine;
using UnityEngine.AI;

namespace TankControl
{
    /// <summary>
    /// Keeps a player-controlled tank on the baked NavMesh without physics colliders.
    /// </summary>
    public sealed class NavMeshMovementConstraint : ITankMovementConstraint
    {
        private const float NavMeshProjectionDistance = 1f;

        public bool CanMove(ITankChassis chassis, Vector2 movement, float deltaTime)
        {
            if (chassis == null)
            {
                return false;
            }

            var movementDelta = chassis.GetMovementDelta(movement.y, movement.x, deltaTime);
            if (movementDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return true;
            }

            if (!NavMesh.SamplePosition(
                    chassis.Position,
                    out var startPosition,
                    NavMeshProjectionDistance,
                    NavMesh.AllAreas))
            {
                return false;
            }

            return !NavMesh.Raycast(
                startPosition.position,
                startPosition.position + movementDelta,
                out _,
                NavMesh.AllAreas);
        }
    }
}
