// Copyright (c) Royal Apps. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
// RoyalTerminal.Tests — coverage for power-user shell UX: command palette,
// broadcast input, fullscreen, scroll commands, and move-tab-to-new-window.

using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using RoyalTerminal.Avalonia.App;
using RoyalTerminal.Avalonia.App.ViewModels;
using Xunit;

namespace RoyalTerminal.Tests;

[Collection("MainWindowControllerHeadlessTests")]
public sealed class MainWindowControllerPowerUxTests
{
    [Fact]
    public void CommandPalette_FiltersBySubstringAndSubsequence()
    {
        MainWindowViewModel viewModel = new();

        viewModel.OpenCommandPaletteCommand.Execute().Subscribe();
        Assert.True(viewModel.IsCommandPaletteVisible);
        Assert.NotEmpty(viewModel.FilteredCommandPaletteItems);

        viewModel.CommandPaletteQuery = "split";
        Assert.Contains(viewModel.FilteredCommandPaletteItems, item => item.Name == "Split Pane Right");
        Assert.Contains(viewModel.FilteredCommandPaletteItems, item => item.Name == "Split Pane Auto");
        Assert.DoesNotContain(viewModel.FilteredCommandPaletteItems, item => item.Name == "Copy");
        Assert.Equal(viewModel.FilteredCommandPaletteItems[0], viewModel.SelectedCommandPaletteItem);

        // Subsequence fallback: "spr" matches "Split Pane Right".
        viewModel.CommandPaletteQuery = "spr";
        Assert.Contains(viewModel.FilteredCommandPaletteItems, item => item.Name == "Split Pane Right");
    }

    [AvaloniaFact]
    public void CommandPalette_ExecuteRunsCommandAndCloses()
    {
        MainWindowViewModel viewModel = new();
        bool executed = false;
        using IDisposable handler = viewModel.CreateNewTabInteraction.RegisterHandler(context =>
        {
            executed = true;
            context.SetOutput(System.Reactive.Unit.Default);
        });

        viewModel.OpenCommandPaletteCommand.Execute().Subscribe();
        viewModel.CommandPaletteQuery = "New Tab";
        viewModel.ExecuteCommandPaletteItem(null);
        Dispatcher.UIThread.RunJobs();

        Assert.True(executed);
        Assert.False(viewModel.IsCommandPaletteVisible);
    }

    [Fact]
    public void CommandPalette_SelectionMovesAndClamps()
    {
        MainWindowViewModel viewModel = new();
        viewModel.OpenCommandPaletteCommand.Execute().Subscribe();

        CommandPaletteItem first = viewModel.FilteredCommandPaletteItems[0];
        viewModel.MoveCommandPaletteSelection(1);
        Assert.Equal(viewModel.FilteredCommandPaletteItems[1], viewModel.SelectedCommandPaletteItem);
        viewModel.MoveCommandPaletteSelection(-1);
        Assert.Equal(first, viewModel.SelectedCommandPaletteItem);
        viewModel.MoveCommandPaletteSelection(-1);
        Assert.Equal(first, viewModel.SelectedCommandPaletteItem);
    }

    [Fact]
    public void KeybindingResolver_ResolvesNewActions()
    {
        MainWindowViewModel viewModel = new();

        Assert.NotNull(ShellKeybindingResolver.Resolve(
            new ShellKeybinding("F11", ShellKeybindingActions.ToggleFullscreen), viewModel));
        Assert.NotNull(ShellKeybindingResolver.Resolve(
            new ShellKeybinding("Ctrl+Shift+P", ShellKeybindingActions.CommandPalette), viewModel));
        Assert.NotNull(ShellKeybindingResolver.Resolve(
            new ShellKeybinding("Ctrl+Shift+Home", ShellKeybindingActions.ScrollToTop), viewModel));
        Assert.NotNull(ShellKeybindingResolver.Resolve(
            new ShellKeybinding("Alt+Shift+B", ShellKeybindingActions.ToggleBroadcastInput), viewModel));
        Assert.NotNull(ShellKeybindingResolver.Resolve(
            new ShellKeybinding("Ctrl+Alt+D1", ShellKeybindingActions.NewTabProfileIndex, "0"), viewModel));
        Assert.Null(ShellKeybindingResolver.Resolve(
            new ShellKeybinding("Ctrl+Alt+D1", ShellKeybindingActions.NewTabProfileIndex, "not-a-number"), viewModel));
    }
}
