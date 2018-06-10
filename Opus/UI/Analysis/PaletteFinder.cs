using System.Drawing;

namespace Opus.UI.Analysis
{
    public abstract class PaletteFinder
    {
        protected const int NumPalettes = 4;

        /// <summary>
        /// Distance from the top of a palette header to the top of the "body" of the palette.
        /// </summary>
        protected const int PaletteHeaderHeight = 40;

        /// <summary>
        /// Distance from the top of one header to the next when the palette is completely "closed".
        /// </summary>
        protected const int PaletteHeaderSeparationHeight = 50;

        /// <summary>
        /// Distance between the "bottom" of a palette and the top of the next one when the palette
        /// is completely "open".
        /// </summary>
        protected const int PaletteBottomSeparationHeight = 2;

        /// <summary>
        /// Distance from the bottom of the "body" of the last palette to the bottom of the sidebar.
        /// </summary>
        protected const int PaletteFooterHeight = 10;


        public int TotalScrollableHeight { get; protected set; }

        public abstract DisposableList<PaletteInfo> Analyze();

        protected Rectangle SidebarRect { get; private set; }

        protected PaletteFinder(Rectangle sidebarRect)
        {
            SidebarRect = sidebarRect;
        }
    }
}