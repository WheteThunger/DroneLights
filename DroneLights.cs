using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Drone Lights", "WhiteThunder", "1.0.0")]
    [Description("Adds a controllable search light to deployable drones.")]
    internal class DroneLights : CovalencePlugin
    {
        #region Fields

        private static DroneLights _pluginInstance;

        private const string PermissionAutoDeploy = "dronelights.searchlight.autodeploy";

        private const string SpherePrefab = "assets/prefabs/visualization/sphere.prefab";
        private const string SearchLightPrefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab";

        private static readonly Vector3 SphereEntityInitialLocalPosition = new Vector3(0, -200, 0);
        private static readonly Vector3 SphereEntityLocalPosition = new Vector3(0, -0.075f, 0.25f);
        private static readonly Vector3 SearchLightLocalPosition = new Vector3(0, -1.25f, -0.25f);
        private static readonly Quaternion SearchLightLocalRotation = Quaternion.Euler(0, 180, 0);
        private static readonly Vector3 SearchLightDefaultAimDir = new Vector3(0, -0.2f, 0);

        private const float VerticalAimSensitivity = 15f;
        private const float MinVerticalAngle = -0.5f;
        private const float MaxVerticalAngle = 0.5f;

        private readonly Dictionary<uint, Vector3> _savedAimDirs = new Dictionary<uint, Vector3>();
        private ProtectionProperties ImmortalProtection;

        #endregion

        #region Hooks

        private void Init()
        {
            _pluginInstance = this;
            permission.RegisterPermission(PermissionAutoDeploy, this);
            Unsubscribe(nameof(OnEntitySpawned));
        }

        private void Unload()
        {
            UnityEngine.Object.Destroy(ImmortalProtection);
            _pluginInstance = null;
        }

        private void OnServerInitialized()
        {
            ImmortalProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
            ImmortalProtection.name = "DroneLightsProtection";
            ImmortalProtection.Add(1);

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                var drone = entity as Drone;
                if (drone == null || !IsDroneEligible(drone))
                    continue;

                AddOrUpdateSearchLight(drone);
            }

            Subscribe(nameof(OnEntitySpawned));
        }

        private void OnEntitySpawned(Drone drone)
        {
            if (!IsDroneEligible(drone))
                return;

            MaybeAutoDeploySearchLight(drone);
        }

        private void OnEntityKill(Drone drone)
        {
            if (!IsDroneEligible(drone))
                return;

            _savedAimDirs.Remove(drone.net.ID);
        }

        private void OnBookmarkInput(ComputerStation station, BasePlayer player, InputState inputState)
        {
            var drone = GetControlledDrone(station);
            if (drone == null)
                return;

            var searchLight = GetDroneSearchLight(drone);
            if (searchLight == null)
                return;

            if (!searchLight.HasFlag(IOEntity.Flag_HasPower))
                return;

            var delta = inputState.current.mouseDelta.y * Time.deltaTime * VerticalAimSensitivity;

            var newAimDir = drone.transform.TransformDirection(Vector3.forward);
            newAimDir.y = Clamp(searchLight.aimDir.y + delta, MinVerticalAngle, MaxVerticalAngle);
            searchLight.aimDir = newAimDir;
            searchLight.SendNetworkUpdateImmediate();
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.cmd.FullName != "inventory.lighttoggle")
                return null;

            var basePlayer = arg.Player();
            if (basePlayer == null)
                return null;

            var station = basePlayer.GetMounted() as ComputerStation;
            if (station == null)
                return null;

            var drone = GetControlledDrone(station);
            if (drone == null)
                return null;

            var searchLight = GetDroneSearchLight(drone);
            if (searchLight == null)
                return null;

            var wasOn = searchLight.HasFlag(IOEntity.Flag_HasPower);
            searchLight.SetFlag(IOEntity.Flag_HasPower, !wasOn, networkupdate: false);

            if (wasOn)
            {
                _savedAimDirs[drone.net.ID] = searchLight.aimDir;
                searchLight.ResetState();
            }
            else
                searchLight.aimDir = drone.transform.TransformDirection(GetInitialAimDir(drone.net.ID));

            searchLight.SendNetworkUpdate();

            // Prevent nightvision since it's not useful while viewing the computer station.
            return false;
        }

        #endregion

        #region Helper Methods

        private static bool DeployLightWasBlocked(Drone drone)
        {
            object hookResult = Interface.CallHook("OnDroneSearchLightDeploy", drone);
            return hookResult is bool && (bool)hookResult == false;
        }

        private static bool IsDroneEligible(Drone drone) =>
            !(drone is DeliveryDrone);

        private static Drone GetControlledDrone(ComputerStation station) =>
            station.currentlyControllingEnt.Get(serverside: true) as Drone;

        private static SearchLight GetDroneSearchLight(Drone drone) =>
            GetGrandChildOfType<SphereEntity, SearchLight>(drone);

        private static T2 GetGrandChildOfType<T1, T2>(BaseEntity entity) where T1 : BaseEntity where T2 : BaseEntity
        {
            foreach (var child in entity.children)
            {
                var childOfType = child as T1;
                if (childOfType == null)
                    continue;

                foreach (var grandChild in childOfType.children)
                {
                    var grandChildOfType = grandChild as T2;
                    if (grandChildOfType != null)
                        return grandChildOfType;
                }
            }
            return null;
        }

        private static void RemoveProblemComponents(BaseEntity entity)
        {
            foreach (var collider in entity.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(collider);

            UnityEngine.Object.DestroyImmediate(entity.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<GroundWatch>());
        }

        private static void HideInputsAndOutputs(IOEntity ioEntity)
        {
            // Trick to hide the inputs and outputs on the client.
            foreach (var input in ioEntity.inputs)
                input.type = IOEntity.IOType.Generic;

            foreach (var output in ioEntity.outputs)
                output.type = IOEntity.IOType.Generic;
        }

        private static float Clamp(float x, float min, float max) => Math.Max(min, Math.Min(x, max));

        private SearchLight TryDeploySearchLight(Drone drone)
        {
            if (DeployLightWasBlocked(drone))
                return null;

            // Spawn the search light below the map initially while the resize is performed.
            SphereEntity sphereEntity = GameManager.server.CreateEntity(SpherePrefab, SphereEntityInitialLocalPosition) as SphereEntity;
            if (sphereEntity == null)
                return null;

            // Fix the issue where leaving the area and returning would not recreate the sphere and its children on clients.
            sphereEntity.globalBroadcast = false;

            sphereEntity.currentRadius = 0.1f;
            sphereEntity.lerpRadius = 0.1f;

            sphereEntity.SetParent(drone);
            sphereEntity.Spawn();

            SearchLight searchLight = GameManager.server.CreateEntity(SearchLightPrefab, SearchLightLocalPosition, SearchLightLocalRotation) as SearchLight;
            if (searchLight == null)
                return null;

            SetupSearchLight(searchLight);

            searchLight.SetParent(sphereEntity);
            searchLight.Spawn();
            Interface.CallHook("OnDroneSearchLightDeployed", drone, searchLight);

            timer.Once(3, () =>
            {
                if (sphereEntity != null)
                    sphereEntity.transform.localPosition = SphereEntityLocalPosition;
            });

            return searchLight;
        }

        private void SetupSearchLight(SearchLight searchLight)
        {
            RemoveProblemComponents(searchLight);
            HideInputsAndOutputs(searchLight);
            searchLight.SetFlag(BaseEntity.Flags.Busy, true);
            searchLight.baseProtection = ImmortalProtection;
            searchLight.pickup.enabled = false;
        }

        private void AddOrUpdateSearchLight(Drone drone)
        {
            var searchLight = GetDroneSearchLight(drone);
            if (searchLight == null)
            {
                MaybeAutoDeploySearchLight(drone);
                return;
            }

            SetupSearchLight(searchLight);
        }

        private void MaybeAutoDeploySearchLight(Drone drone)
        {
            if (!permission.UserHasPermission(drone.OwnerID.ToString(), PermissionAutoDeploy))
                return;

            TryDeploySearchLight(drone);
        }

        private Vector3 GetInitialAimDir(uint droneId)
        {
            Vector3 aimDir;
            return _savedAimDirs.TryGetValue(droneId, out aimDir)
                ? aimDir
                : SearchLightDefaultAimDir;
        }

        #endregion
    }
}
