// Copyright (c) Royal Apps. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
// RoyalTerminal.Avalonia.App - Host-configurable shell keybindings.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Input;
using RoyalTerminal.Avalonia.App.ViewModels;

namespace RoyalTerminal.Avalonia.App;

/// <summary>
/// A host-supplied key binding: a parseable key gesture bound to a named
/// shell action, with an optional argument (e.g. a tab index).
/// </summary>
/// <param name="Gesture">Avalonia key gesture text, e.g. "Ctrl+Shift+T".</param>
/// <param name="ActionId">One of the <see cref="ShellKeybindingActions"/> ids.</param>
/// <param name="Argument">Optional action argument.</param>
public sealed record ShellKeybinding(string Gesture, string ActionId, string? Argument = null);

/// <summary>
/// Action ids resolvable by the shell for host-supplied keybindings.
/// </summary>
public static class ShellKeybindingActions
{
    public const string NewTab = "newTab";
    public const string NewWindow = "newWindow";
    public const string CloseTab = "closeTab";
    public const string ClosePane = "closePane";
    public const string DuplicateTab = "duplicateTab";
    public const string NextTab = "nextTab";
    public const string PrevTab = "prevTab";
    public const string SwitchToTab = "switchToTab";
    public const string SplitRight = "splitRight";
    public const string SplitDown = "splitDown";
    public const string SplitAuto = "splitAuto";
    public const string MoveFocusLeft = "moveFocusLeft";
    public const string MoveFocusRight = "moveFocusRight";
    public const string MoveFocusUp = "moveFocusUp";
    public const string MoveFocusDown = "moveFocusDown";
    public const string ResizePaneLeft = "resizePaneLeft";
    public const string ResizePaneRight = "resizePaneRight";
    public const string ResizePaneUp = "resizePaneUp";
    public const string ResizePaneDown = "resizePaneDown";
    public const string TogglePaneZoom = "togglePaneZoom";
    public const string Copy = "copy";
    public const string Paste = "paste";
    public const string SelectAll = "selectAll";
    public const string Find = "find";
    public const string IncreaseFontSize = "increaseFontSize";
    public const string DecreaseFontSize = "decreaseFontSize";
    public const string ResetFontSize = "resetFontSize";
}

/// <summary>
/// Resolves host-supplied keybindings to shell commands.
/// </summary>
internal static class ShellKeybindingResolver
{
    /// <summary>
    /// Resolves a binding to an Avalonia <see cref="KeyBinding"/>, or null
    /// when the gesture is unparseable or the action id is unknown.
    /// </summary>
    public static KeyBinding? Resolve(ShellKeybinding binding, MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(viewModel);

        KeyGesture gesture;
        try
        {
            gesture = KeyGesture.Parse(binding.Gesture);
        }
        catch (Exception)
        {
            return null;
        }

        (System.Windows.Input.ICommand? command, object? parameter) = binding.ActionId switch
        {
            ShellKeybindingActions.NewTab => (viewModel.NewTabCommand, null),
            ShellKeybindingActions.NewWindow => (viewModel.NewWindowCommand, null),
            ShellKeybindingActions.CloseTab => (viewModel.CloseCurrentTabCommand, null),
            ShellKeybindingActions.ClosePane => (viewModel.ClosePaneOrTabCommand, null),
            ShellKeybindingActions.DuplicateTab => (viewModel.DuplicateTabCommand, null),
            ShellKeybindingActions.NextTab => (viewModel.CycleTabForwardCommand, null),
            ShellKeybindingActions.PrevTab => (viewModel.CycleTabBackwardCommand, null),
            ShellKeybindingActions.SwitchToTab => ResolveSwitchToTab(viewModel, binding.Argument),
            ShellKeybindingActions.SplitRight => (viewModel.SplitPaneRightCommand, null),
            ShellKeybindingActions.SplitDown => (viewModel.SplitPaneDownCommand, null),
            ShellKeybindingActions.SplitAuto => (viewModel.SplitPaneAutoCommand, null),
            ShellKeybindingActions.MoveFocusLeft => (viewModel.FocusPaneLeftCommand, null),
            ShellKeybindingActions.MoveFocusRight => (viewModel.FocusPaneRightCommand, null),
            ShellKeybindingActions.MoveFocusUp => (viewModel.FocusPaneUpCommand, null),
            ShellKeybindingActions.MoveFocusDown => (viewModel.FocusPaneDownCommand, null),
            ShellKeybindingActions.ResizePaneLeft => (viewModel.ResizePaneLeftCommand, null),
            ShellKeybindingActions.ResizePaneRight => (viewModel.ResizePaneRightCommand, null),
            ShellKeybindingActions.ResizePaneUp => (viewModel.ResizePaneUpCommand, null),
            ShellKeybindingActions.ResizePaneDown => (viewModel.ResizePaneDownCommand, null),
            ShellKeybindingActions.TogglePaneZoom => (viewModel.TogglePaneZoomCommand, null),
            ShellKeybindingActions.Copy => (viewModel.CopySelectionCommand, null),
            ShellKeybindingActions.Paste => (viewModel.PasteClipboardCommand, null),
            ShellKeybindingActions.SelectAll => (viewModel.SelectAllCommand, null),
            ShellKeybindingActions.Find => (viewModel.ToggleSearchPanelCommand, null),
            ShellKeybindingActions.IncreaseFontSize => (viewModel.IncreaseFontSizeCommand, null),
            ShellKeybindingActions.DecreaseFontSize => (viewModel.DecreaseFontSizeCommand, null),
            ShellKeybindingActions.ResetFontSize => (viewModel.ResetFontSizeCommand, null),
            _ => ((System.Windows.Input.ICommand?)null, (object?)null),
        };

        if (command is null)
        {
            return null;
        }

        KeyBinding keyBinding = new()
        {
            Gesture = gesture,
            Command = command,
        };
        if (parameter is not null)
        {
            keyBinding.CommandParameter = parameter;
        }

        return keyBinding;
    }

    private static (System.Windows.Input.ICommand?, object?) ResolveSwitchToTab(
        MainWindowViewModel viewModel,
        string? argument)
    {
        if (!int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) || index < 0)
        {
            return (null, null);
        }

        return (viewModel.SwitchToTabByIndexCommand, index.ToString(CultureInfo.InvariantCulture));
    }
}
