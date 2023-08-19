# NUMovement
### Description
NUMovement is a custom Character Controller implementation designed with the intent of having it be easy to modify and tailor to the worlds specific purpose. The package depends on UdonSharp, and it comes with the NUMovement prefab, a supplementary movement system similar to VRChats, which users should hopefully feel familiar with. The NUMovement system comes with features such as cancelling jumps early, sliding down slopes, moving and rotating with platforms, automatically jumping as long as jump is held down, flight collider prevention and more.

The NUMovement class is derived from AbstractMovement, an abstract base class, which handles most of the variable caching boilerplate and providing useful player information in a performant manner. Initially this project started off with the intent of creating an example system that beginners could use as a reference for implementing their own movement systems. However, I've also made most of the logic accessible and overridable from derived classes, in case you want to keep most of the behaviour from the NUMovement class but maybe implement something like double jumping. There is also an abstract class called AbstractMovementCollider which handles determining if it was the NUMovement controller which entered it for the sake of convenience. The AbstractMovementCollider is also implemented in a few example scripts that come with the package.

### Features
* Jump cancelling.
* Auto jump.
* Sliding down slopes.
* Moving and rotating with platforms.
* Platform inertia.
* Ground snapping.
* Flight collider prevention.
* Avatar height based movement scaling.
* Reflects most of the VRCPlayerApi.
* Visuals to debug the system.

### Requirements
* [Udon](https://vrchat.com/home/download)
* [UdonSharp](https://github.com/vrchat-community/UdonSharp)
