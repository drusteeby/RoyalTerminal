// Copyright (c) Royal Apps. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
// Tests for shifted printable key encoding under the kitty keyboard protocol.

using System.Text;
using RoyalTerminal.Avalonia.Rendering;
using RoyalTerminal.Terminal;
using Xunit;

namespace RoyalTerminal.Tests;

public sealed class GhosttyShiftedDigitKittyEncodingTests
{
    [Theory]
    [InlineData("D8", "*")]
    [InlineData("D1", "!")]
    [InlineData("D3", "#")]
    [InlineData("A", "A")]
    public void TryEncodeKey_ShiftedPrintableWithDisambiguate_SendsShiftedCharacter(string keyId, string text)
    {
        // Multiplexers such as herdr push kitty "disambiguate" (flag 1). A
        // shifted printable key must still send the shifted character, not
        // the base key: Shift+8 is "*", never "8" or CSI 56;2u.
        using GhosttyVtProcessor processor = new(new TerminalScreen(80, 24, 0));
        processor.Process(Encoding.ASCII.GetBytes("\u001b[>1u"));

        bool encoded = processor.TryEncodeKey(
            new TerminalKeyEncodingRequest(keyId, TerminalInputAction.Press, text, TerminalModifiers.Shift),
            out byte[] sequence);

        Assert.True(encoded);
        Assert.Equal(text, Encoding.UTF8.GetString(sequence));
    }

    [Fact]
    public void TryEncodeKey_ShiftedDigitWithReportAllKeys_StillSendsEscapeSequence()
    {
        // Apps that ask for every key as an escape code (flag 8) must keep
        // getting CSI u for Shift+8 rather than bare text.
        using GhosttyVtProcessor processor = new(new TerminalScreen(80, 24, 0));
        processor.Process(Encoding.ASCII.GetBytes("\u001b[>9u"));

        bool encoded = processor.TryEncodeKey(
            new TerminalKeyEncodingRequest("D8", TerminalInputAction.Press, "*", TerminalModifiers.Shift),
            out byte[] sequence);

        Assert.True(encoded);
        Assert.StartsWith("\u001b[56", Encoding.UTF8.GetString(sequence));
    }
}
