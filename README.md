# Prefab Placer Tool

Prefab Placer Tool is a Unity utility for procedural prefab placement on terrains. It supports multiple generation modes (Quad, Circle, Sphere, Mesh) and uses asynchronous Poisson Disk Sampling to generate spawn points. The tool also offers terrain cleanup (trees and details) in rotated, circular (or cylindrical), and mesh-based areas.

## Features

- **Multiple Generation Modes:** Create points in rectangular, circular, spherical, or mesh-based areas.
- **Terrain Cleanup:** Remove trees and details from specified areas, with rotation support.
- **Custom Inspectors & Gizmos:** Visualize generation areas and configure prefab parameters.
- **UniRX Dependency:** Requires UniRX for reactive properties.

## Requirements

- Unity 2019.4 or later.
- UniRX package.

## Usage

1. Attach the `PrefabPlacer` component to a GameObject.
2. Configure the terrain and prefab settings.
3. Use the context menu options to generate or clear prefabs.
4. Enjoy your procedurally generated environment!

## License

This project is licensed under the MIT License.