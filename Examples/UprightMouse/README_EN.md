# Upright Mouse

[简体中文](README.md)

This pack adds an upright mouse as a wearable player appearance. Select it in the Appearances menu to give your character a mouse head, ears, body, and a swaying tail. Your character keeps the game's movement and clothing behavior.

## Try it in the game

The example includes a Windows x64 model bundle. Build and install the [Appearance Mod Framework](../AppearanceFramework/README_EN.md#build-and-try-it). Copy `mod.json`, `appearance.json`, the [cover image](preview.png), and `Assets/uprightmouse.bundle` to `Mods/UprightMouse`, keeping the `Assets/` folder.

Enable the framework and Upright Mouse in the mod list. Quit and reopen the game. On an island, press **F10** and choose Upright Mouse under Body. Walk, turn, and try on some of the game's clothes. To try it with a friend, both players need the framework. A player without the pack can see it when automatic reception of room appearances is allowed.

## Change the colors

Open [appearance.json](appearance.json) in your installed copy. In `materials`, `MouseFur` is the fur, `EarPink` the inner ears, `TailPink` the tail, `MouseNose` the nose, and `EyeWhite` and `EyePupil` the eyes. Change a `color` to a value such as `"#C8875C"`. Save the file, restart the game, and select the appearance again.

The framework uses the game's mouse materials to keep the original lighting and shadows. The example sets the colors in the configuration. It also shows how to attach parts, hide the original visuals, and configure blinking and tail motion. [Appearance data](../AppearanceFramework/README_EN.md#make-an-appearance-pack)

## Edit the model or make your own character

1. In Unity Hub, create a **Unity 2022.3.62f3c1** Universal 3D (URP) project and install Windows Build Support. The preview materials use URP 14.0.12. Copy the **contents** of [Source/UnityAssets](Source/UnityAssets) into the new project's `Assets` folder, including the provided `.meta` files.
2. Open `Assets/UprightMouse/Preview.unity` to see the assembled model. Edit and save part positions in `Assets/UprightMouse/UprightMouse.prefab`. To change the meshes, export new FBX files from the [Blender source](Source/Models/UprightMouse.blend). The tail's editable bones are in [UprightMouseTail.fbx](Source/UnityAssets/UprightMouse/Models/UprightMouseTail.fbx).
3. Select the prefab in the Unity Project window and choose **Cozy Island → Build Selected Prefab Bundle (Windows)**. If you are editing Upright Mouse, save the file as `uprightmouse.bundle`, replace the bundle in your test copy's `Assets` folder, and restart the game to check it.
4. To make a new appearance, copy this example. In `mod.json`, give the mod a new UUID and name. In `appearance.json`, set your own pack and entry IDs and names. Update `bundle`, `prefab`, and `parts` in `appearance.json` with your bundle path, prefab path, and model parts. Start with a new head. Put a model named `Head` under the prefab root and use a single part `{"path":"Head","anchor":"Face","hideOriginal":true}`. Remove the mouse-specific `materials`, `bodyMaterial`, and `hideBody` fields. Check the new head in the game, then add more parts and colors. [Appearance data](../AppearanceFramework/README_EN.md#make-an-appearance-pack)
