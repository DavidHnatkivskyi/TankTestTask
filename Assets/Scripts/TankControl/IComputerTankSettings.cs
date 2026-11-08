namespace TankControl
{
    /// <summary>
    /// Read-only tuning values for computer-controlled tank behavior.
    /// </summary>
    public interface IComputerTankSettings
    {
        float ComputerPatrolRadius { get; }

        int NavMeshPointSearchAttempts { get; }

        float ComputerOrbitMinimumRadius { get; }

        float ComputerOrbitMaximumRadius { get; }

        int ComputerOrbitPointCount { get; }

        float ComputerOrbitPointArrivalDistance { get; }

        float ComputerFireMinimumDistance { get; }

        float ComputerFireMaximumDistance { get; }
        float ComputerFireAlignmentAngle { get; }
        float ComputerWaypointArrivalDistance { get; }
        float ComputerPathRetryDelay { get; }
        float ComputerPlayerPathRefreshDuration { get; }
        float ComputerOrbitPointProjectionDistance { get; }
    }
}
