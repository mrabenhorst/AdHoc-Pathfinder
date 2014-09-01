AdHoc-Pathfinder
================

AdHoc Pathfinder (AHP) is a free and open-source pathfinder solution for use in Unity Applications. It specializes in high performance and low memory overhead for vast->infinite terrain surfaces. AHP calculates a path based on initial conditions without the need for a prebaked or precalculated mesh. Instead, AHP uses an artifical grid and raycasting to determine a position's walkable state.

#Features:

  * Pathfinding without the memory overhead of a baked mesh
  * Can be used on terrain of any size without loss of performance
  * Multi-terrain compatible (I use and recommend TerrainComposer)
  * Automatically detects terrain edges
  * Completely accessible via code (C# now, may translate to JS later)
  * Stupid simple to operate out of the box
  * Outputs raw Vector3 coordinate list rather than forcing you to use it's own movement system
 
  

#Planned features: (Which may or may not make it to release)

  * Planetary point-gravity pathfinding compatibility (I can't afford the asset to test yet. ;.; )
  * Multi-level support for voxel terrains (I can't afford a voxel asset to test yet. ;.; )
  * Behavior list (Patrolling, fleeing, etc)
  * Better movement behaviors
  * Better heuristics options
