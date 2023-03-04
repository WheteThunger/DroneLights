## Features

- Automatically adds a search light to deployable drones (requires permission)
- Allows remotely toggling the light on and off with the standard key bind
- Allows remotely aiming the light with mouse movements (requires permission)

## Installation

1. Add the plugin to the `oxide/plugins` directory of your Rust server installation
2. Grant the `dronelights.searchlight.autodeploy` permission to players or groups whose drones should have search lights
3. Reload the plugin if you want to automatically add lights to any existing drones (if they were deployed by players with the above permission)

Note: The search light entity may not be visible in most cases due to client-side issues with parenting entities. However, rest assured it is present and the light can be toggled while the drone is being controlled at a computer station. The light beam will be visible even if the search light entity is not.

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
    "DefaultAngle": 75
  }
}
```

- `SearchLight`
  - `DefaultAngle` (`0` - `180`) -- The default angle that the search light will be aiming when spawned.
    - `0` = Down
    - `90` = Forward
    - `180` = Up

## FAQ

#### How do I toggle the light on and off?

Drone lights use the same command (or key bind) for toggling nightvision, flashlights and vehicle lights. The client-side console command for this is `lighttoggle`, which is bound to the `f` key by default. You can rebind it using the F1 client console or by using the in-game options menu.

## Recommended compatible plugins

Drone balance:
- [Drone Settings](https://umod.org/plugins/drone-settings) -- Allows changing speed, toughness and other properties of RC drones.
- [Targetable Drones](https://umod.org/plugins/targetable-drones) -- Allows RC drones to be targeted by Auto Turrets and SAM Sites.
- [Limited Drone Range](https://umod.org/plugins/limited-drone-range) -- Limits how far RC drones can be controlled from computer stations.

Drone fixes and improvements:
- [Better Drone Collision](https://umod.org/plugins/better-drone-collision) -- Overhauls RC drone collision damage so it's more intuitive.
- [Auto Flip Drones](https://umod.org/plugins/auto-flip-drones) -- Auto flips upside-down RC drones when a player takes control.
- [Drone Hover](https://umod.org/plugins/drone-hover) -- Allows RC drones to hover in place while not being controlled.

Drone attachments:
- [Drone Lights](https://umod.org/plugins/drone-lights) (This plugin) -- Adds controllable search lights to RC drones.
- [Drone Turrets](https://umod.org/plugins/drone-turrets) -- Allows players to deploy auto turrets to RC drones.
- [Drone Storage](https://umod.org/plugins/drone-storage) -- Allows players to deploy a small stash to RC drones.
- [Ridable Drones](https://umod.org/plugins/ridable-drones) -- Allows players to ride RC drones by standing on them or mounting a chair.

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
