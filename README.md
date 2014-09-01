#AdHoc-Pathfinder
================

AdHoc Pathfinder (AHP) is a free and open-source pathfinder solution for use in Unity Applications. It specializes in high performance and low memory overhead for vast->infinite terrain surfaces. AHP calculates a path based on initial conditions without the need for a prebaked or precalculated mesh. Instead, AHP uses an artifical grid and raycasting to determine a position's walkable state.

###Features:

  * Pathfinding without the memory overhead of a baked mesh
  * Can be used on terrain of any size without loss of performance
  * Multi-terrain compatible (I use and recommend TerrainComposer)
  * Automatically detects terrain edges
  * Completely accessible via code (C# now, may translate to JS later)
  * Stupid simple to operate out of the box
  * Outputs raw Vector3 coordinate list rather than forcing you to use it's own movement system
 
  

###Planned features:

  * Planetary point-gravity pathfinding compatibility (I can't afford the asset to test yet. ;.; )
  * Multi-level support for voxel terrains (I can't afford a voxel asset to test yet. ;.; )
  * Behavior list (Patrolling, fleeing, etc)
  * Better movement behaviors
  * Better heuristics options


###What does AHP have that other pathfinders don't?
AHP is a niche pathfinder. It is designed for people who:

 * Use massive terrains
 * Don't want the memory overhead of a baked mesh

Because AHP generates a grid on the fly, performance is not based on the size of the terrain, rather, it is based on the complexity and length of the desired path. So if your path is 50 units long and it's on a terrain 100x100, it will perform as fast as if it was on a terrain of 100,000x100,000 but it won't use any more memory.

###How does it work?
AHP It accepts a maximum slope and "Out Of Bounds" (OOB) tags to create a path on command that fits the requirements. It uses a combination of a predetermined grid (specified by the resolution) and raycasting to conduct the pathfinding.

###Is it the shortest path?
Yes and No. It CAN be. For performance reasons, AHP is not designed to always return the shortest path - rather, a path that fits the requirements. However, different Heuristics will be available so the user can balance performance->accuracy as needed.
