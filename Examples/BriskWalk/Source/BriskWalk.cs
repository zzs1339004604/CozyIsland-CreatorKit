using System;
using System.Collections.Generic;
using System.IO;
using CozyIsland.Modding;
using PhysicsBasedCharacterController;
using UnityEngine;
namespace CozyMods.Examples {
    public sealed class BriskWalk : ICozyMod {
        [Serializable] sealed class Settings { public float walkMultiplier = 1.25f; public float runMultiplier = 1.15f; }
        readonly Dictionary<PCDMovementMgr, float> walks = new Dictionary<PCDMovementMgr, float>();
        readonly Dictionary<CharacterManager, float> runs = new Dictionary<CharacterManager, float>();
        public void OnLoad(CozyModContext context) {
            var settings = JsonUtility.FromJson<Settings>(File.ReadAllText(context.ReadPath("walking.json")));
            if (settings == null || !Valid(settings.walkMultiplier) || !Valid(settings.runMultiplier)) throw new InvalidDataException("Movement multipliers must be finite values between 0.25 and 3");
            float remaining = 0;
            context.OnUpdate(delta => {
                if (!context.IsHost || (remaining -= delta) > 0) return;
                remaining = .5f;
                foreach (var player in UnityEngine.Object.FindObjectsOfType<CozyPlayer>()) {
                    var movement = player.GetComponent<PCDMovementMgr>();
                    if (movement != null && !walks.ContainsKey(movement)) { walks.Add(movement, movement.walkSpeed); movement.walkSpeed *= settings.walkMultiplier; }
                    var character = player.GetComponent<CharacterManager>();
                    if (character != null && !runs.ContainsKey(character)) { runs.Add(character, character.sprintSpeed); character.sprintSpeed *= settings.runMultiplier; }
                }
            });
            context.AddCleanup(Restore);
        }
        static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= .25f && value <= 3;
        public void OnUnload() { Restore(); }
        void Restore() {
            foreach (var entry in walks) if (entry.Key != null) entry.Key.walkSpeed = entry.Value;
            foreach (var entry in runs) if (entry.Key != null) entry.Key.sprintSpeed = entry.Value;
            walks.Clear(); runs.Clear();
        }
    }
}
