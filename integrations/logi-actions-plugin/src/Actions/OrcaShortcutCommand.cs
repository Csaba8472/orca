namespace Loupedeck.OrcaPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // Sends Orca's default chords (src/shared/keybindings/definitions-core-*.ts); rebound actions need their keys reassigned.
    public class OrcaShortcutCommand : PluginDynamicCommand
    {
        private sealed record Chord(VirtualKeyCode Key, Boolean Mod = false, Boolean Shift = false, Boolean Alt = false);

        private sealed record Shortcut(String Id, String DisplayName, Chord Default, Chord Mac = null)
        {
            public Chord ForCurrentPlatform => OperatingSystem.IsMacOS() && this.Mac != null ? this.Mac : this.Default;
        }

        private static readonly Shortcut[] Shortcuts =
        {
            new("workspace.create", "New Worktree", new Chord(VirtualKeyCode.KeyN, Mod: true)),
            new("worktree.palette", "Switch Worktree", new Chord(VirtualKeyCode.KeyJ, Mod: true, Shift: true), Mac: new Chord(VirtualKeyCode.KeyJ, Mod: true)),
            new("worktree.navigateUp", "Previous Worktree", new Chord(VirtualKeyCode.ArrowUp, Mod: true, Shift: true)),
            new("worktree.navigateDown", "Next Worktree", new Chord(VirtualKeyCode.ArrowDown, Mod: true, Shift: true)),
            new("worktree.history.back", "Worktree History Back", new Chord(VirtualKeyCode.ArrowLeft, Mod: true, Alt: true)),
            new("worktree.history.forward", "Worktree History Forward", new Chord(VirtualKeyCode.ArrowRight, Mod: true, Alt: true)),
            new("sidebar.left.toggle", "Toggle Sidebar", new Chord(VirtualKeyCode.KeyB, Mod: true)),
            new("sidebar.right.toggle", "Toggle Right Sidebar", new Chord(VirtualKeyCode.KeyL, Mod: true)),
            new("sidebar.sourceControl.toggle", "Source Control", new Chord(VirtualKeyCode.KeyG, Mod: true, Shift: true)),
            new("floatingTerminal.toggle", "Floating Terminal", new Chord(VirtualKeyCode.KeyA, Mod: true, Alt: true)),
            new("tab.newTerminal", "New Terminal Tab", new Chord(VirtualKeyCode.KeyT, Mod: true)),
            new("tab.close", "Close Tab", new Chord(VirtualKeyCode.KeyW, Mod: true)),
        };

        private static readonly Dictionary<String, Shortcut> ShortcutsById = Shortcuts.ToDictionary(shortcut => shortcut.Id);

        public OrcaShortcutCommand()
            : base()
        {
            foreach (var shortcut in Shortcuts)
            {
                this.AddParameter(shortcut.Id, shortcut.DisplayName, "Navigation");
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (!ShortcutsById.TryGetValue(actionParameter, out var shortcut))
            {
                return;
            }

            var chord = shortcut.ForCurrentPlatform;
            this.Plugin.ClientApplication.SendKeyboardShortcut(chord.Key, Modifiers(chord));
        }

        private static ModifierKey Modifiers(Chord chord)
        {
            var modifiers = ModifierKey.None;
            if (chord.Mod)
            {
                // Orca's "Mod" is Cmd on macOS and Ctrl elsewhere.
                modifiers |= OperatingSystem.IsMacOS() ? ModifierKey.Command : ModifierKey.Control;
            }
            if (chord.Shift)
            {
                modifiers |= ModifierKey.Shift;
            }
            if (chord.Alt)
            {
                modifiers |= ModifierKey.Alt;
            }
            return modifiers;
        }
    }
}
