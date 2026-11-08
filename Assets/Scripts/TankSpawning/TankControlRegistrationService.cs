using System;
using CameraControl;
using Tank;
using TankControl;

namespace TankSpawning
{
    internal sealed class TankControlRegistrationService
    {
        private readonly IUserTankControlTargetRegistry _userTankRegistry;
        private readonly IComputerTankControlTargetRegistry _computerTankRegistry;
        private readonly IPlayerTankCameraTargetRegistry _cameraTargetRegistry;

        public TankControlRegistrationService(
            IUserTankControlTargetRegistry userTankRegistry,
            IComputerTankControlTargetRegistry computerTankRegistry,
            IPlayerTankCameraTargetRegistry cameraTargetRegistry)
        {
            _userTankRegistry = userTankRegistry;
            _computerTankRegistry = computerTankRegistry;
            _cameraTargetRegistry = cameraTargetRegistry;
        }

        public void Register(TankSpawnHandle handle)
        {
            switch (handle.ControlType)
            {
                case TankControlType.LocalPlayer:
                    _userTankRegistry.Register(handle.Chassis, handle.Turret);
                    _computerTankRegistry.SetPlayerTarget(handle.Chassis);
                    _cameraTargetRegistry.Register(handle.Chassis, handle.Turret);
                    break;
                case TankControlType.Computer:
                    RegisterComputer(handle);
                    break;
            }
        }

        public void Unregister(TankSpawnHandle handle)
        {
            switch (handle.ControlType)
            {
                case TankControlType.LocalPlayer:
                    _userTankRegistry.Unregister(handle.Chassis);
                    _computerTankRegistry.ClearPlayerTarget(handle.Chassis);
                    _cameraTargetRegistry.Unregister(handle.Chassis);
                    break;
                case TankControlType.Computer:
                    _computerTankRegistry.UnregisterComputer(handle.Chassis);
                    break;
            }
        }

        private void RegisterComputer(TankSpawnHandle handle)
        {
            if (handle.ComputerNavigation == null)
            {
                throw new InvalidOperationException(
                    $"Computer tank prefab '{handle.SpawnData.Prefab.name}' requires {nameof(IComputerTankNavigation)}.");
            }

            _computerTankRegistry.RegisterComputer(
                handle.Chassis,
                handle.Turret,
                handle.ComputerNavigation,
                handle.SpawnData.ComputerBehavior);
        }
    }
}
