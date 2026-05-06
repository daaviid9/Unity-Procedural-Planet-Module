# Procedural Planet Generator

A modular Unity package for generating high-quality procedural planets with LOD support, biomes, and custom shader effects. Designed to be easily integrated into any Unity project via the Package Manager.

## Features
- **Dynamic Mesh Generation**: Multi-threaded (Unity Job System compatible structure) terrain generation based on cube-sphere topology.
- **Level of Detail (LOD)**: Proximity-based resolution scaling for optimal performance.
- **Biome System**: Elevation and noise-based biome distribution.
- **Customizable Shaders**: Shader Graph-based terrain rendering with support for multiple terrain layers (Sand, Grass, Mountain, Snow).
- **Flexible Settings**: ScriptableObject-based configuration for shape, color, and noise.
- **Orbit Camera**: Built-in smooth camera controller with zoom, vertical limits, and auto-rotation support.

## Installation

### Via Git URL
1. Open the Unity Package Manager (`Window > Package Manager`).
2. Click the `+` icon and select `Add package from git URL...`.
3. Paste the URL of this repository.

### Via Local Disk
1. Download or clone this repository to your machine.
2. In the Unity Package Manager, click `+` and select `Add package from disk...`.
3. Select the `package.json` file inside the `ProceduralPlanet` folder.

## Quick Start Guide

1. **Create a Planet Object**:
   - Right-click in the Hierarchy and create a new **Empty GameObject**.
   - Add the `Planet` component to the object.
   
2. **Assign Settings**:
   - Create or assign **Shape Settings** and **Color Settings** (found in the `Runtime/PlanetSettings` folder of the package).
   - Assign a **Material** that uses the `PlanetTerrain` shader.

3. **Configure Camera**:
   - Add the `OrbitCamera` component to your **Main Camera**.
   - Assign your Planet as the `Target` (or leave empty to auto-detect).
   - Assign the **Main Camera** (or any viewer transform) to the `Viewer` field in the Planet component to ensure LOD works correctly.

4. **Generate**:
   - Click the **Generate Planet** button in the inspector or enable **Auto Update**.

## Technical Details
- **Namespace**: `ProceduralPlanet`
- **Assembly**: `ProceduralPlanet.Runtime`, `ProceduralPlanet.Editor`
- **Unity Version**: 2020.3+

## License
MIT License - Created for Bachelor's Thesis project.
