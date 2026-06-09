using System;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace AvClientView.Multiplayer;

/// <summary>
/// Shared lobby View utilities.
/// </summary>
internal static class LobbyHelper
{
    /// <summary>
    /// Sets up hover tracking on a game list. Calls <paramref name="setHoveredIndex"/>
    /// with the estimated item index under the pointer.
    /// </summary>
    public static void SetUpHoverTracking(ListBox listBox, Action<int> setHoveredIndex)
    {
        listBox.AddHandler(InputElement.PointerMovedEvent, (_, e) =>
        {
            var pos = e.GetPosition(listBox);
            int idx = (int)(pos.Y / 48.0);
            if (idx < 0) idx = 0;
            setHoveredIndex(idx);
        }, handledEventsToo: true);

        listBox.AddHandler(InputElement.PointerExitedEvent, (_, _) =>
        {
            setHoveredIndex(-1);
        }, handledEventsToo: true);
    }

    /// <summary>
    /// Auto-scrolls the list to the end when new items extend the content,
    /// but only if the user was already at the bottom.
    /// </summary>
    public static void AutoScrollToEnd(ListBox listBox)
    {
        bool setup = false;
        void Setup()
        {
            if (setup) return;
            var sv = listBox.FindDescendantOfType<ScrollViewer>();
            if (sv is null) return;
            setup = true;

            bool isAtBottom = true;
            double prevExtent = 0;

            sv.ScrollChanged += (_, _) =>
            {
                bool wasAtBottom = isAtBottom;
                isAtBottom = sv.Offset.Y >= sv.Extent.Height - sv.Viewport.Height - 2;
                if (sv.Extent.Height > prevExtent && wasAtBottom)
                    sv.ScrollToEnd();
                prevExtent = sv.Extent.Height;
            };

            sv.ScrollToEnd();
        }

        listBox.TemplateApplied += (_, _) => Setup();
        listBox.LayoutUpdated += (_, _) => Setup();
    }
}
