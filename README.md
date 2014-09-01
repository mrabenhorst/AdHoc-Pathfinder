#AdHoc-Pathfinder
================

AdHoc Pathfinder (AHP) is a free and open-source pathfinder solution for use in Unity Applications. It specializes in high performance and low memory overhead for vast->infinite terrain surfaces. AHP calculates a path based on initial conditions without the need for a prebaked or precalculated mesh. Instead, AHP uses an artifical grid and raycasting to determine a position's walkable state.

<form action="https://www.paypal.com/cgi-bin/webscr" method="post" target="_top">
<input type="hidden" name="cmd" value="_s-xclick">
<input type="hidden" name="encrypted" value="-----BEGIN PKCS7-----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-----END PKCS7-----
">
<input type="image" src="https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif" border="0" name="submit" alt="PayPal - The safer, easier way to pay online!">
<img alt="" border="0" src="https://www.paypalobjects.com/en_US/i/scr/pixel.gif" width="1" height="1">
</form>


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
