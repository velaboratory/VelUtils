1. Install the package. Instructions for this can be found on the [homepage](../index.md)
 - Add the InputMan component to any GameObject in your scene (like your game manager object)
 - Click the button on InputMan to add the default input module.
 - Either drag in the "Rig" prefab found under Packages/VelUtils/Runtime/Prefabs/Rig, or create your own rig:
     1. Add the `Rig.cs` component to your main rig parent object
     - Assign the head and hand transforms. These can be the children of the tracked objects if you want to apply an offset
     - Add the `Movement.cs` component to your main rig parent object (the same object as `Rig.cs`)
     - Assign your Rig to the Movement movement component
 - Adjust your Movement settings to match the movement you want