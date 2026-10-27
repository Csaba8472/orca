namespace Loupedeck.OrcaPlugin
{
    using System;

    internal static class WorktreeKeyRenderer
    {
        // Mirrors the Orca sidebar status dots (dark theme) so a key reads like its sidebar row.
        private static readonly BitmapColor Background = new BitmapColor(23, 23, 23);
        private static readonly BitmapColor ActiveOutline = new BitmapColor(250, 250, 250);
        private static readonly BitmapColor PrimaryText = new BitmapColor(250, 250, 250);
        private static readonly BitmapColor MutedText = new BitmapColor(163, 163, 163);
        private static readonly BitmapColor Working = new BitmapColor(237, 178, 0);
        private static readonly BitmapColor Permission = new BitmapColor(254, 110, 0);
        private static readonly BitmapColor Done = new BitmapColor(0, 188, 125);
        private static readonly BitmapColor Failed = new BitmapColor(251, 44, 54);
        private static readonly BitmapColor Idle = new BitmapColor(64, 64, 64);

        public static BitmapImage Render(OrcaWorktree worktree, Boolean orcaReachable, PluginImageSize imageSize)
        {
            var size = KeyPixels(imageSize);
            using var builder = new BitmapBuilder(size, size);
            builder.Clear(Background);

            if (!orcaReachable)
            {
                builder.DrawText("Orca\noffline", 0, 0, size, size, MutedText, size / 7);
                return builder.ToImage();
            }
            if (worktree == null)
            {
                builder.DrawText("—", 0, 0, size, size, Idle, size / 5);
                return builder.ToImage();
            }

            var band = size / 8;
            builder.FillRectangle(0, 0, size, band, StatusColor(worktree.Status));
            if (worktree.IsActive)
            {
                var edge = Math.Max(2, size / 40);
                builder.FillRectangle(0, size - edge, size, edge, ActiveOutline);
                builder.FillRectangle(0, band, edge, size - band, ActiveOutline);
                builder.FillRectangle(size - edge, band, edge, size - band, ActiveOutline);
            }

            var repoHeight = size / 4;
            builder.DrawText(Truncate(worktree.Repo, 12), 0, band, size, repoHeight, MutedText, size / 9);
            builder.DrawText(
                Truncate(worktree.DisplayName, 18),
                0,
                band + repoHeight,
                size,
                size - band - repoHeight,
                PrimaryText,
                size / 7);
            return builder.ToImage();
        }

        private static BitmapColor StatusColor(String status) => status switch
        {
            "working" or "monitoring" => Working,
            "permission" => Permission,
            "done" or "active" => Done,
            "failed" => Failed,
            _ => Idle
        };

        private static String Truncate(String text, Int32 max) =>
            text.Length <= max ? text : text.Substring(0, max - 1) + "…";

        private static Int32 KeyPixels(PluginImageSize imageSize) => imageSize switch
        {
            PluginImageSize.Width90 => 80,
            _ => 116
        };
    }
}
