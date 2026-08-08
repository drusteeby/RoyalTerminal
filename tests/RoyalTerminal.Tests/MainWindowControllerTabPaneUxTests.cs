// Copyright (c) Royal Apps. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
// RoyalTerminal.Tests — coverage for mouse-centric tab/pane shell UX
// (middle-click close, inline rename, duplicate tab, pane zoom,
// close-pane-or-tab, and host-injected theme presets).

using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RoyalTerminal.Avalonia.Rendering;
using RoyalTerminal.Avalonia.Services;
using RoyalTerminal.Avalonia.App;
using RoyalTerminal.Avalonia.App.Services;
using RoyalTerminal.Avalonia.App.ViewModels;
using RoyalTerminal.Terminal;
using RoyalTerminal.Terminal.Theming;
using ReactiveUI;
using Xunit;

namespace RoyalTerminal.Tests;

[Collection("MainWindowControllerHeadlessTests")]
public sealed class MainWindowControllerTabPaneUxTests
{
    private const string DisableSessionAutostartEnvVar = "ROYALTERMINAL_DEMO_DISABLE_SESSION_AUTOSTART";

    private const string CampbellThemeText = """
        foreground = #CCCCCC
        background = #0C0C0C
        cursor = #FFFFFF
        color0 = #0C0C0C
        color1 = #C50F1F
        color2 = #13A10E
        color3 = #C19C00
        color4 = #0037DA
        color5 = #881798
        color6 = #3A96DD
        color7 = #CCCCCC
        color8 = #767676
        color9 = #E74856
        color10 = #16C60C
        color11 = #F9F1A5
        color12 = #3B78FF
        color13 = #B4009E
        color14 = #61D6D6
        color15 = #F2F2F2
        """;

    [Fact]
    public void ThemeCatalog_MergesAdditionalPresetsAheadOfBuiltIns()
    {
        TerminalThemeCatalog catalog = new(
            [new ShellThemePreset("wt-campbell", "Campbell", CampbellThemeText)],
            defaultPresetId: "wt-campbell");

        Assert.Equal("wt-campbell", catalog.Presets[0].Id);
        Assert.True(catalog.Presets.Count > 1);
        Assert.Equal("wt-campbell", catalog.GetDefaultPreset(TerminalRenderMode.RenderedAuto).Id);
        Assert.Equal("wt-campbell", catalog.GetDefaultPreset(TerminalRenderMode.NativeVt).Id);
    }

    [Fact]
    public void ThemeCatalog_ParsesInjectedPresetColors()
    {
        TerminalThemeCatalog catalog = new(
            [new ShellThemePreset("wt-campbell", "Campbell", CampbellThemeText)],
            defaultPresetId: "wt-campbell");

        TerminalTheme theme = catalog.CreatePresetTheme("wt-campbell", TerminalRenderMode.RenderedAuto);

        Assert.Equal(0xFF0C0C0Cu, theme.DefaultBackground);
        Assert.Equal(0xFFCCCCCCu, theme.DefaultForeground);
    }

    [Fact]
    public void ThemeCatalog_WithoutAdditionalPresets_KeepsBuiltInDefaults()
    {
        TerminalThemeCatalog catalog = new();

        Assert.Equal("gruvbox-dark", catalog.GetDefaultPreset(TerminalRenderMode.NativeVt).Id);
    }

    [AvaloniaFact]
    public void MiddleClickRelease_OnTabHeader_ClosesTab()
    {
        using IDisposable autostart = SetProcessEnvironmentVariable(DisableSessionAutostartEnvVar, "1");
        (MainWindowViewModel viewModel, MainWindowController controller, Window window, Grid terminalHost, ItemsControl tabStrip)
            = CreateActivatedController(out IDisposable lifetime);
        try
        {
            viewModel.NewTabCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, tabStrip.ItemCount);

            Button firstHeader = GetTabHeader(tabStrip, 0);
            RaisePointerReleased(firstHeader, MouseButton.Middle);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, tabStrip.ItemCount);
        }
        finally
        {
            lifetime.Dispose();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void DuplicateTabCommand_AddsTab()
    {
        using IDisposable autostart = SetProcessEnvironmentVariable(DisableSessionAutostartEnvVar, "1");
        (MainWindowViewModel viewModel, MainWindowController controller, Window window, Grid terminalHost, ItemsControl tabStrip)
            = CreateActivatedController(out IDisposable lifetime);
        try
        {
            Assert.Equal(1, tabStrip.ItemCount);
            viewModel.DuplicateTabCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, tabStrip.ItemCount);
        }
        finally
        {
            lifetime.Dispose();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TogglePaneZoom_MaximizesActivePane_AndRestores()
    {
        using IDisposable autostart = SetProcessEnvironmentVariable(DisableSessionAutostartEnvVar, "1");
        (MainWindowViewModel viewModel, MainWindowController controller, Window window, Grid terminalHost, ItemsControl tabStrip)
            = CreateActivatedController(out IDisposable lifetime);
        try
        {
            viewModel.SplitPaneRightCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();
            int hostChildrenAfterSplit = terminalHost.Children.Count;
            Control tabContainer = Assert.IsAssignableFrom<Control>(terminalHost.Children[0]);
            Assert.True(tabContainer.IsVisible);

            viewModel.TogglePaneZoomCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(hostChildrenAfterSplit + 1, terminalHost.Children.Count);
            Assert.False(tabContainer.IsVisible);

            viewModel.TogglePaneZoomCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(hostChildrenAfterSplit, terminalHost.Children.Count);
            Assert.True(tabContainer.IsVisible);
        }
        finally
        {
            lifetime.Dispose();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SplitPane_WhileZoomed_RestoresLayoutFirst()
    {
        using IDisposable autostart = SetProcessEnvironmentVariable(DisableSessionAutostartEnvVar, "1");
        (MainWindowViewModel viewModel, MainWindowController controller, Window window, Grid terminalHost, ItemsControl tabStrip)
            = CreateActivatedController(out IDisposable lifetime);
        try
        {
            viewModel.SplitPaneRightCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();
            viewModel.TogglePaneZoomCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();
            int zoomedChildren = terminalHost.Children.Count;

            viewModel.SplitPaneDownCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();

            // The zoom host is removed while the tab container remains.
            Assert.Equal(zoomedChildren - 1, terminalHost.Children.Count);
            Control tabContainer = Assert.IsAssignableFrom<Control>(terminalHost.Children[0]);
            Assert.True(tabContainer.IsVisible);
        }
        finally
        {
            lifetime.Dispose();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ClosePaneOrTab_WithSplitPane_ClosesOnlyThePane()
    {
        using IDisposable autostart = SetProcessEnvironmentVariable(DisableSessionAutostartEnvVar, "1");
        (MainWindowViewModel viewModel, MainWindowController controller, Window window, Grid terminalHost, ItemsControl tabStrip)
            = CreateActivatedController(out IDisposable lifetime);
        try
        {
            viewModel.SplitPaneRightCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, tabStrip.ItemCount);

            viewModel.ClosePaneOrTabCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, tabStrip.ItemCount);
        }
        finally
        {
            lifetime.Dispose();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ClosePaneOrTab_WithSinglePane_ClosesTab()
    {
        using IDisposable autostart = SetProcessEnvironmentVariable(DisableSessionAutostartEnvVar, "1");
        (MainWindowViewModel viewModel, MainWindowController controller, Window window, Grid terminalHost, ItemsControl tabStrip)
            = CreateActivatedController(out IDisposable lifetime);
        try
        {
            viewModel.NewTabCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, tabStrip.ItemCount);

            viewModel.ClosePaneOrTabCommand.Execute().Subscribe();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, tabStrip.ItemCount);
        }
        finally
        {
            lifetime.Dispose();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TabHeader_HasContextMenuWithWtActions()
    {
        using IDisposable autostart = SetProcessEnvironmentVariable(DisableSessionAutostartEnvVar, "1");
        (MainWindowViewModel viewModel, MainWindowController controller, Window window, Grid terminalHost, ItemsControl tabStrip)
            = CreateActivatedController(out IDisposable lifetime);
        try
        {
            Button header = GetTabHeader(tabStrip, 0);
            Assert.NotNull(header.ContextMenu);
            List<string> headers = header.ContextMenu!.Items
                .OfType<MenuItem>()
                .Select(item => item.Header?.ToString() ?? string.Empty)
                .ToList();
            Assert.Contains("Rename Tab", headers);
            Assert.Contains("Duplicate Tab", headers);
            Assert.Contains("Close Other Tabs", headers);
            Assert.Contains("Close Tabs to the Right", headers);
        }
        finally
        {
            lifetime.Dispose();
            window.Close();
        }
    }

    private static (MainWindowViewModel ViewModel, MainWindowController Controller, Window Window, Grid TerminalHost, ItemsControl TabStrip)
        CreateActivatedController(out IDisposable lifetime)
    {
        MainWindowViewModel viewModel = new();
        Window window = CreateControllerHostWindow(viewModel, out Grid terminalHost);
        MainWindowController controller = new(
            window,
            viewModel,
            new TerminalModeCapabilityResolver(),
            TerminalModeResolver.Default,
            settingsProfileStore: new InMemoryProfileStore(new TerminalSessionProfilesDocument()),
            workspaceStore: new InMemoryWorkspaceStore());
        lifetime = controller.Activate();
        Dispatcher.UIThread.RunJobs();
        ItemsControl tabStrip = window.FindControl<ItemsControl>("TabStrip")
            ?? throw new InvalidOperationException("TabStrip not found.");
        return (viewModel, controller, window, terminalHost, tabStrip);
    }

    private static Button GetTabHeader(ItemsControl tabStrip, int index)
    {
        Dispatcher.UIThread.RunJobs();
        List<Button> headers = tabStrip.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Classes.Contains("tabHeader"))
            .ToList();
        Assert.True(headers.Count > index, $"Expected at least {index + 1} tab headers, found {headers.Count}.");
        return headers[index];
    }

    private static void RaisePointerReleased(Control target, MouseButton button)
    {
        Point position = target.Bounds.Center;
        Visual root = TopLevel.GetTopLevel(target) ?? throw new InvalidOperationException("Control is not attached.");
        target.RaiseEvent(new PointerReleasedEventArgs(
            target,
            new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true),
            root,
            target.TranslatePoint(position, root) ?? position,
            0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.MiddleButtonReleased),
            KeyModifiers.None,
            button));
    }

    private static IDisposable SetProcessEnvironmentVariable(string variable, string? value)
    {
        string? original = Environment.GetEnvironmentVariable(variable);
        Environment.SetEnvironmentVariable(variable, value);
        return new EnvironmentVariableRestorer(variable, original);
    }

    private sealed class EnvironmentVariableRestorer(string variable, string? original) : IDisposable
    {
        public void Dispose() => Environment.SetEnvironmentVariable(variable, original);
    }

    private sealed class InMemoryProfileStore(TerminalSessionProfilesDocument document) : ITerminalSessionProfileStore
    {
        private TerminalSessionProfilesDocument _document = document;

        public ValueTask<TerminalSessionProfilesDocument> LoadAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(_document);

        public ValueTask SaveAsync(TerminalSessionProfilesDocument document, CancellationToken cancellationToken = default)
        {
            _document = document;
            return ValueTask.CompletedTask;
        }
    }

    private static Window CreateControllerHostWindow(MainWindowViewModel viewModel, out Grid terminalHost)
    {
        Border titleBarBrandIcon = new()
        {
            Name = "TitleBarBrandIcon",
            IsVisible = viewModel.IsTitleBarLogoVisible,
        };
        titleBarBrandIcon.Bind(Visual.IsVisibleProperty, viewModel.WhenAnyValue(static model => model.IsTitleBarLogoVisible));

        ContentControl titleBarTabStripHost = new()
        {
            Name = "TitleBarTabStripHost",
            IsVisible = viewModel.IsTabsInTitleBar,
        };
        titleBarTabStripHost.Bind(Visual.IsVisibleProperty, viewModel.WhenAnyValue(static model => model.IsTabsInTitleBar));

        ItemsControl tabStrip = new()
        {
            Name = "TabStrip",
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel { Orientation = Orientation.Horizontal }),
        };
        RepeatButton tabStripScrollLeftButton = new()
        {
            Name = "TabStripScrollLeftButton",
            IsVisible = false,
        };
        ScrollViewer tabStripScrollViewer = new()
        {
            Name = "TabStripScrollViewer",
            Content = tabStrip,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        RepeatButton tabStripScrollRightButton = new()
        {
            Name = "TabStripScrollRightButton",
            IsVisible = false,
        };
        Button tabStripNewTabButton = new()
        {
            Name = "TabStripNewTabButton",
            Command = viewModel.NewTabCommand,
        };
        StackPanel windowsCaptionButtonStrip = CreateWindowsCaptionButtonStrip(
            out Button captionMinimizeButton,
            out Button captionMaximizeButton,
            out Button captionRestoreButton,
            out Button captionFullscreenButton,
            out Button captionCloseButton);
        WindowDecorationProperties.SetElementRole(tabStripScrollLeftButton, WindowDecorationsElementRole.User);
        WindowDecorationProperties.SetElementRole(tabStripScrollRightButton, WindowDecorationsElementRole.User);
        WindowDecorationProperties.SetElementRole(tabStripNewTabButton, WindowDecorationsElementRole.User);
        WindowDecorationProperties.SetElementRole(windowsCaptionButtonStrip, WindowDecorationsElementRole.User);

        Grid tabStripLayout = new()
        {
            Name = "TabStripLayout",
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
            },
        };
        tabStripLayout.Children.Add(tabStripScrollLeftButton);
        tabStripLayout.Children.Add(tabStripScrollViewer);
        tabStripLayout.Children.Add(tabStripScrollRightButton);
        tabStripLayout.Children.Add(tabStripNewTabButton);
        Grid.SetColumn(tabStripScrollViewer, 1);
        Grid.SetColumn(tabStripScrollRightButton, 2);
        Grid.SetColumn(tabStripNewTabButton, 3);

        Border tabStripSurface = new()
        {
            Name = "TabStripSurface",
            Child = tabStripLayout,
        };

        ContentControl bodyTabStripHost = new()
        {
            Name = "BodyTabStripHost",
            Content = tabStripSurface,
            IsVisible = viewModel.IsBodyTabStripVisible,
        };
        bodyTabStripHost.Bind(Visual.IsVisibleProperty, viewModel.WhenAnyValue(static model => model.IsBodyTabStripVisible));

        terminalHost = new Grid
        {
            Name = "TerminalHost",
        };

        Grid root = new();
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        Grid titleBar = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto),
            },
        };
        titleBar.Children.Add(titleBarBrandIcon);
        titleBar.Children.Add(titleBarTabStripHost);
        titleBar.Children.Add(windowsCaptionButtonStrip);
        Grid.SetColumn(titleBarTabStripHost, 1);
        Grid.SetColumn(windowsCaptionButtonStrip, 2);
        root.Children.Add(titleBar);
        root.Children.Add(bodyTabStripHost);
        root.Children.Add(terminalHost);
        Grid.SetRow(titleBar, 0);
        Grid.SetRow(bodyTabStripHost, 1);
        Grid.SetRow(terminalHost, 2);

        Window window = new()
        {
            Width = 1200,
            Height = 800,
            DataContext = viewModel,
            Content = root,
        };

        NameScope nameScope = new();
        NameScope.SetNameScope(window, nameScope);
        nameScope.Register(tabStrip.Name!, tabStrip);
        nameScope.Register(titleBarBrandIcon.Name!, titleBarBrandIcon);
        nameScope.Register(titleBarTabStripHost.Name!, titleBarTabStripHost);
        nameScope.Register(bodyTabStripHost.Name!, bodyTabStripHost);
        nameScope.Register(tabStripSurface.Name!, tabStripSurface);
        nameScope.Register(tabStripLayout.Name!, tabStripLayout);
        nameScope.Register(tabStripScrollLeftButton.Name!, tabStripScrollLeftButton);
        nameScope.Register(tabStripScrollViewer.Name!, tabStripScrollViewer);
        nameScope.Register(tabStripScrollRightButton.Name!, tabStripScrollRightButton);
        nameScope.Register(tabStripNewTabButton.Name!, tabStripNewTabButton);
        nameScope.Register(windowsCaptionButtonStrip.Name!, windowsCaptionButtonStrip);
        nameScope.Register(captionMinimizeButton.Name!, captionMinimizeButton);
        nameScope.Register(captionMaximizeButton.Name!, captionMaximizeButton);
        nameScope.Register(captionRestoreButton.Name!, captionRestoreButton);
        nameScope.Register(captionFullscreenButton.Name!, captionFullscreenButton);
        nameScope.Register(captionCloseButton.Name!, captionCloseButton);
        nameScope.Register(terminalHost.Name!, terminalHost);

        window.Show();
        window.Focus();
        return window;
    }

    private static StackPanel CreateWindowsCaptionButtonStrip(
        out Button minimizeButton,
        out Button maximizeButton,
        out Button restoreButton,
        out Button fullscreenButton,
        out Button closeButton)
    {
        minimizeButton = new Button { Name = "CaptionMinimizeButton" };
        maximizeButton = new Button { Name = "CaptionMaximizeButton" };
        restoreButton = new Button { Name = "CaptionRestoreButton" };
        fullscreenButton = new Button { Name = "CaptionFullscreenButton" };
        closeButton = new Button { Name = "CaptionCloseButton" };

        StackPanel strip = new()
        {
            Name = "WindowsCaptionButtonStrip",
            Orientation = Orientation.Horizontal,
        };
        strip.Children.Add(minimizeButton);
        strip.Children.Add(maximizeButton);
        strip.Children.Add(restoreButton);
        strip.Children.Add(fullscreenButton);
        strip.Children.Add(closeButton);
        return strip;
    }
}
