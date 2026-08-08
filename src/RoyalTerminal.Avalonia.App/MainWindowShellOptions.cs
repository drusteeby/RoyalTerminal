// Copyright (c) Royal Apps. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
// RoyalTerminal.Avalonia.App - Reusable terminal shell options.

namespace RoyalTerminal.Avalonia.App;

using RoyalTerminal.Avalonia.App.Services;
using RoyalTerminal.Terminal;

/// <summary>
/// Describes a host-supplied terminal theme preset in Ghostty theme text format
/// (foreground/background/cursor/selection keys and color0..color15 entries).
/// </summary>
/// <param name="Id">Stable preset identifier.</param>
/// <param name="DisplayName">Name shown by shell theme UI.</param>
/// <param name="ThemeText">Ghostty-style theme definition text.</param>
public sealed record ShellThemePreset(string Id, string DisplayName, string ThemeText);

/// <summary>
/// Configures host-specific presentation options for the reusable main window shell.
/// </summary>
public sealed class MainWindowShellOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether macOS title-bar RoyalTerminal logos are shown.
    /// </summary>
    public bool ShowMacOsTitleBarLogos { get; set; } = true;

    /// <summary>
    /// Gets or sets the app-level preference store used by the shell.
    /// </summary>
    public IAppPreferencesStore? AppPreferencesStore { get; set; }

    /// <summary>
    /// Gets or sets host-supplied terminal theme presets listed ahead of the
    /// built-in presets.
    /// </summary>
    public IReadOnlyList<ShellThemePreset>? AdditionalThemePresets { get; set; }

    /// <summary>
    /// Gets or sets the preset id used as the default theme for every render
    /// mode. When null, built-in per-mode defaults apply.
    /// </summary>
    public string? DefaultThemePresetId { get; set; }

    /// <summary>
    /// Gets or sets the session profile store backing the shell's profile
    /// catalog. When null, the platform default store is used.
    /// </summary>
    public ITerminalSessionProfileStore? SessionProfileStore { get; set; }

    /// <summary>
    /// Gets or sets host-supplied keybindings. When set, they replace the
    /// built-in window keybindings entirely.
    /// </summary>
    public IReadOnlyList<ShellKeybinding>? Keybindings { get; set; }

    /// <summary>
    /// Gets or sets the factory invoked by the "new window" action. Null
    /// disables the action.
    /// </summary>
    public Action? NewWindowFactory { get; set; }

    /// <summary>
    /// Gets or sets whether closing a window that hosts multiple tabs asks
    /// for confirmation first.
    /// </summary>
    public bool ConfirmCloseAllTabs { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the left tool rail starts visible. Terminal-first
    /// hosts can start collapsed; View > Show Left Panel toggles it back.
    /// </summary>
    public bool ShowLeftPanelOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets the action invoked by "open settings" (e.g. opening the
    /// host's settings file in an editor). Null opens the in-app settings
    /// panel instead.
    /// </summary>
    public Action? OpenSettingsAction { get; set; }

    /// <summary>
    /// Gets or sets the factory invoked by "move tab to new window" with the
    /// tab's profile id and working directory. Null disables the action.
    /// </summary>
    public Action<string?, string?>? MoveTabToNewWindowFactory { get; set; }

    /// <summary>
    /// Gets or sets the profile id used for the first tab instead of the
    /// default profile (used by "move tab to new window").
    /// </summary>
    public string? InitialProfileId { get; set; }

    /// <summary>
    /// Gets or sets a working directory override for the first tab.
    /// </summary>
    public string? InitialWorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the workspace store used to persist and restore
    /// tab/pane layouts across launches. Null uses the platform default.
    /// </summary>
    public ITerminalWorkspaceStore? WorkspaceStore { get; set; }
}
