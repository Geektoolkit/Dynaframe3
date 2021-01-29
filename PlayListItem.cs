using System;
using System.Collections.Generic;
using System.Text;

namespace Dynaframe3
{
    public enum PlayListItemType { Image, Video, AnimatedGif, Invalid}
    public class PlayListItem : IEquatable<PlayListItem>
    {
        public string Path { get; set; }
        public PlayListItemType ItemType { get; set; }

        /// <summary>
        /// Returns true if the path and itemtime are equal
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(PlayListItem other)
        {
            return this.Path == other.Path &&
                this.ItemType == other.ItemType;
        }

    }


}
