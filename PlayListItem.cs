using System;
using System.Collections.Generic;
using System.Text;

namespace Dynaframe3
{
    public enum PlayListItemType { Image, Video, AnimatedGif, Invalid}
    public class PlayListItem
    {
        public string Path { get; set; }
        public PlayListItemType ItemType { get; set; }

    }
}
