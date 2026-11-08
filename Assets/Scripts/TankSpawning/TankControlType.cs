namespace TankSpawning
{
    /// <summary>
    /// Identifies which system owns a spawned tank.
    /// </summary>
    public enum TankControlType
    {
        LocalPlayer = 0,
        Computer = 1,
        Photon = 2
    }
}
