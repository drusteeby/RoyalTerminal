// Copyright (c) Royal Apps. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
// RoyalTerminal.Avalonia.Controls - Right mouse button behavior.

namespace RoyalTerminal.Avalonia.Controls;

/// <summary>
/// Controls what the right mouse button does inside a terminal.
/// </summary>
public enum TerminalRightClickMode
{
    /// <summary>
    /// Reports the click to mouse-tracking applications and opens the host
    /// context menu.
    /// </summary>
    ContextMenu,

    /// <summary>
    /// Reports the click to mouse-tracking applications (e.g. a multiplexer)
    /// but never opens the host context menu.
    /// </summary>
    Application,

    /// <summary>
    /// Ignores the right button entirely: no context menu and nothing is
    /// reported to the application.
    /// </summary>
    Disabled,
}
