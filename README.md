# Unity Utilities

*Anton Franzluebbers*

This is a collection of utility scripts and assets that I have created that are applicable to a variety of projects.

## Scripts included

### `AdjustPos.cs`
Nudges an object with assignable keyboard shortcuts.

### `InputMan.cs`
Makes input from VR devices accessible from a unified set of methods. Can treat axes as button down.

### `Logger.cs`
Logs any data to a file.

### `CopyTransform.cs`
One object copies the position and/or rotation of another using a variety of techniques. Global or local offsets can be set.

### `SaveLoc.cs`
Saves the location and rotation of an object to playerprefs. Local or global coordinates.

### `Movement.cs`
Adds several movement techniques while maintaining compatibility with many rig setups. (Only contains echo movement for now)

### `Editor/SetupVRInput.cs`
Adds a dropdown menu option for automatically populating the Input system for use with the InputMan script for use with a VR device.

### `Editor/EditorShortcutKeys.cs`
Adds `F5` shortcut to enter play mode.

### `Editor/CopyTransformEditor.cs`
Sets up the interface for the CopyTransform script.


## Usage
Use `mklink Link Target` to use without copying, but this isn't compatible with git.

## TODO
 - add support for local space offsets in CopyTransorm
 - add vibration support in InputMan
 - finish all methods in InputMan
