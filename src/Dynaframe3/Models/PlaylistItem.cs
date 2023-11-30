using System;

namespace Dynaframe3.Models
{
  public enum PlaylistItemType
  {
    Image,
    Video,
    AnimatedGif,
    Invalid
  }

  public class PlaylistItem : IEquatable<PlaylistItem>
  {
    public string Path { get; set; }

    public PlaylistItemType ItemType { get; set; }

    public string Title { get; set; }

    public string Keywords { get; set; }

    public string Software { get; set; }

    public string Artist { get; set; }

    public string Comment { get; set; }

    /// <summary>
    /// Returns true if the path and item time are equal
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(PlaylistItem other)
    {
      return Path == other.Path && ItemType == other.ItemType;
    }
  }
}
