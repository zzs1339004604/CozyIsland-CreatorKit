# Upright Mouse

[简体中文](README.md)

Choose Upright Mouse in Appearances to wear a mouse head, blinking eyes, ears, and a tail that sways as you move. Your character can still wear the game's clothes.

## Try it in the game

1. Install and enable [Appearance Mod Framework](../AppearanceFramework/README_EN.md) 0.2.0 or later. Subscribe to Upright Mouse and enable it too. [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3844597180)
2. Restart the game. Open **Appearances** in the main menu or press **F10** on an island. Choose **Upright Mouse** under **Body**.

To install from the repository, copy `mod.json`, `appearance.json`, `appearance-upgrades.json`, `preview.png`, and [Assets/upright-mouse.glb](Assets/upright-mouse.glb) to `Mods/UprightMouse`. Keep the `Assets/` folder. Windows x64 and macOS use the same model. Friends can receive appearances shared in a room when they use the same version of the framework.

## Change the colors

Open [appearance.json](appearance.json). In `materials`, `MouseFur` colors the fur, `EarPink` the inner ears, `TailPink` the tail, `MouseNose` the nose, and `EyeWhite` and `EyePupil` the eyes. Change a `color` to a value like `"#C8875C"`. Save, restart the game, and equip the appearance to check your changes.

The game applies these colors to its own mouse materials, keeping the original lighting and shadows. The configuration also identifies each attachment point, the blink shape, and the five tail bones.

## Edit the model or make your own body appearance

1. Import [upright-mouse.glb](Assets/upright-mouse.glb) into Blender to view the model used in the game. For the editable meshes, open [UprightMouse.blend](Source/Models/UprightMouse.blend). The tail rig is also available in [UprightMouseTail.fbx](Source/UnityAssets/UprightMouse/Models/UprightMouseTail.fbx).
2. Export a single GLB with the mesh normals, the eye's `Close` shape key, and the tail bones and weights. If you rename or move a node, update the matching `prefab`, `parts`, or `followThrough` path in `appearance.json`. Replace the model in your test copy, restart, and check blinking, walking, and clothing.
3. For a new appearance, use your own name, UUID, and entry IDs and remove the example's `appearance-upgrades.json`. You can start with one node named `Head` and one part, `{"path":"Head","anchor":"Face","hideOriginal":true}`. Update the materials and visibility settings to fit your model. [Appearance data](../AppearanceFramework/README_EN.md#make-an-appearance-pack)
