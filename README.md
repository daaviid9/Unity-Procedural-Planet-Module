# Procedural Planet

Procedural Planet is a Unity package for generating customizable procedural planets. It was created as a bachelor's thesis project and is structured as a standalone Unity Package Manager package.

The package contains the planet generation core, editor tooling, runtime configuration scripts, shader/material assets, terrain textures, preset data, and UI components used by the demo scene.

## Features

- Procedural planet mesh generation based on cube-sphere terrain faces.
- Shape settings with layered noise filters.
- Color, biome, temperature, terrain texture, ocean, and atmosphere-related settings.
- LOD support controlled by viewer distance.
- Custom inspector tools for generating and updating the planet directly in the Unity Editor.
- Runtime UI controllers for changing planet settings in play mode.
- Shader Graph terrain material for Universal Render Pipeline.
- Preset database and simple runtime preset support.
- Orbit camera controller for demo and inspection scenes.

## Package Structure

```text
com.david.proceduralplanet/
  package.json
  README.md
  Runtime/
    Scripts/
    Materials/
    Shaders/
    Prefabs/
    PlanetSettings/
    Presets/
  Editor/
    Scripts/
  Samples~/
    DemoScene/
```

`Runtime` contains the reusable planet system. `Editor` contains editor-only inspector tooling. `Samples~/DemoScene` is intended for the sample scene that demonstrates the runtime UI on top of the core generator.

## Installation

### Git URL

1. Open Unity Package Manager: `Window > Package Manager`.
2. Click `+`.
3. Select `Add package from git URL...`.
4. Enter the repository URL:

```text
https://github.com/daaviid9/Unity-Procedural-Planet-Module.git
```

This repository is a package repository, so `package.json` is located directly in the repository root.

### Local Disk

1. Clone or download this repository.
2. Open Unity Package Manager.
3. Click `+`.
4. Select `Add package from disk...`.
5. Select the root `package.json` file of this repository.

## DemoScene

The package is designed to include a `DemoScene` sample with a ready-to-use runtime UI. The demo scene shows how the planet generator can be controlled during play mode without relying only on the Unity Inspector.

The demo UI demonstrates:

- changing shape and noise settings,
- changing color and biome settings,
- editing terrain texture settings,
- switching settings tabs,
- using runtime presets,
- copying or applying runtime values,
- inspecting the planet with the orbit camera.

If the sample is included under `Samples~/DemoScene`, it can be imported from the Package Manager after installing the package. Select the package, open the `Samples` section, and click `Import`.

## Quick Start

1. Create an empty GameObject.
2. Add the `Planet` component.
3. Assign `ShapeSettings`, `ColorSettings`, and the planet material from the package assets.
4. Assign a viewer transform, usually the main camera, so LOD can update correctly.
5. Use the custom inspector button to generate the planet, or enable automatic updates.

The planet can be configured fully from the Unity Inspector. The runtime UI is an additional layer built on top of the same core settings system.

## Requirements

The package uses Unity systems and packages such as:

- Universal Render Pipeline and Shader Graph,
- TextMeshPro,
- Unity UI,
- Input System,
- Burst,
- Collections,
- Mathematics.

These dependencies should be listed in `package.json` before testing the package in a clean Unity project.

## Technical Details

- Package name: `com.david.proceduralplanet`
- Runtime assembly: `ProceduralPlanet.Runtime`
- Editor assembly: `ProceduralPlanet.Editor`
- Main namespace: `ProceduralPlanet`
- Minimum Unity version declared by the package: `2020.3`

## Thesis Context

This package is part of a bachelor's thesis project focused on procedural planet generation in Unity. The core system was developed first and can be used through the Unity Inspector. The runtime UI and demo scene were added later as a user-facing layer for interactive configuration.

## License

MIT License. Created for a bachelor's thesis project.
