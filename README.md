## Features

- Automatically adds a search light to deployable drones (requires permission)
- Allows remotely toggling the light on and off
- Allows remotely aiming the light with mouse movements (requires permission)

## Installation

1. Add the plugin to the `oxide/plugins` directory of your Rust server installation
2. Grant the `dronelights.searchlight.autodeploy` to players or groups for whom their drones should have search lights
3. Reload the plugin if you want to automatically add lights to any existing drones

Note: The search light entity may not be visible in most cases due to client-side issues with parenting entities. However, rest assured it is present and the light can be toggled while the drone is being controlled at a computer station.

## Permissions

- `dronelights.searchlight.autodeploy` -- Drones deployed by players with this permission will automatically have a search light.
  - Note: Reloading the plugin will automatically add search lights to existing drones owned by players with this permission.
- `dronelights.searchlight.move` -- Allows the player to aim the search light vertically using mouse movements while controlling the drone.
  - Note: Moving the search light has a small performance cost. This is intentionally reported to Oxide as part of the plugin's total hook time so that you can measure the performance impact on your server.

## Configuration

Default configuration:

```json
{
  "SearchLight": {
    "DefaultAngle": 70,
    "MinAngle": 60,
    "MaxAngle": 120,
    "AimSensitivity": 0.25
  }
}
```

- `SearchLight`
  - `DefaultAngle` (`0` - `180`) -- The default angle that the search light will be aiming when spawned.
    - `0` = Down
    - `90` = Forward
    - `180` = Up
  - `MinAngle` (`0` - `180`) -- Min angle players are allowed to aim the search light.
  - `MaxAngle` (`0` - `180`) -- Max angle players are allowed to aim the search light.
  - `AimSensitivity` -- Mouse sensitivity when aiming the search light.

## FAQ

#### How do I get a drone?

As of this writing (February 2021), RC drones can only be made available via admin commands or via plugins.

#### How do I fix drones disconnecting when they get out of range of the computer station?

Install the [Unlimted Drone Range](https://umod.org/plugins/unlimited-drone-range) plugin.

#### How do I toggle the light on and off?

Drone lights use the same command (or key bind) for toggling nightvision, flashlights and vehicle lights. The client-side console command for this is `lighttoggle`, which is bound to the `f` key by default. You can rebind it using the F1 client console or by using the in-game options menu.

## Developer Hooks

#### OnDroneSearchLightDeploy

- Called when this plugin is about to deploy a search light onto a drone
- Returning `false` will prevent the search light from being deployed
- Returning `null` will result in the default behavior

```csharp
object OnDroneSearchLightDeploy(Drone drone)
```

#### OnDroneSearchLightDeployed

- Called after this plugin has deployed a search light onto a drone
- No return behavior

```csharp
void OnDroneSearchLightDeployed(Drone drone, SearchLight searchLight)
```
