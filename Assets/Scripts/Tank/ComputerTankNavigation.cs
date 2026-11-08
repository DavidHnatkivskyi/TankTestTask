using UnityEngine;
using UnityEngine.AI;

namespace Tank
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ComputerTankNavigation : MonoBehaviour, IComputerTankNavigation
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private TankChassis chassis;

        private void Awake()
        {
            if (agent == null || chassis == null)
            {
                throw new MissingReferenceException(
                    $"Assign {nameof(NavMeshAgent)} and {nameof(TankChassis)} to '{name}'.");
            }

        }

        public void Configure()
        {
            agent.speed = chassis.ForwardSpeed;
            agent.angularSpeed = chassis.TurnSpeed;
            if (chassis.DecelerationDuration > 0f)
            {
                agent.acceleration = chassis.ForwardSpeed / chassis.DecelerationDuration;
            }
        }

        public bool HasReachedDestination(float arrivalDistance)
        {
            return agent.isOnNavMesh &&
                   !agent.pathPending &&
                   agent.remainingDistance <= Mathf.Max(arrivalDistance, agent.stoppingDistance);
        }

        public bool TrySetDestination(Vector3 destination)
        {
            if (!agent.isOnNavMesh)
            {
                return false;
            }

            agent.isStopped = false;
            return agent.SetDestination(destination);
        }

        public bool TryGetDestination(out Vector3 destination)
        {
            if (!agent.isOnNavMesh || !agent.hasPath)
            {
                destination = default;
                return false;
            }

            destination = agent.destination;
            return true;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            if (agent.isOnNavMesh)
            {
                agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }

            transform.rotation = rotation;
        }

        public void Stop()
        {
            if (!agent.isOnNavMesh || agent.isStopped && !agent.hasPath)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
