using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Dynaframe3.Models;

namespace Dynaframe3.Playlist
{
  public static class Helpers
  {

    // Fisher Yates shuffle - source: https://stackoverflow.com/questions/273313/randomize-a-listt/4262134#4262134
    // Modified on 3/11 based on A380Coding's feedback. Long lists aren't getting as well shuffled (thousands of images) towards the end with
    // the original algo.
    public static IList<T> Shuffle<T>(this IList<T> list, Random rnd)
    {
      for (var i = list.Count - 1; i > 0; i--)
        list.Swap(i, rnd.Next(list.Count - 1));
      return list;
    }

    /// <summary>
    /// Source: https://stackoverflow.com/questions/3527203/getfiles-with-multiple-extensions
    /// </summary>
    /// <param name="dirInfo"></param>
    /// <param name="extensions"></param>
    /// <returns></returns>
    public static IEnumerable<string> GetFilesByExtensions(this DirectoryInfo dirInfo, SearchOption option, params string[] extensions)
    {
      var allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

      return dirInfo.EnumerateFiles("*.*", option)
                    .Where(f => allowedExtensions.Contains(f.Extension)).Select(s => s.FullName);
    }

    /// <summary>
    /// Returns All supported file types
    /// </summary>
    /// <returns></returns>
    public static string[] GetSupportedExtensions()
    {
      return GetSupportedVideoExtensions().Concat(GetSupportedImageExtensions()).ToArray();
    }

    /// <summary>
    /// Returns the supported Video Extensions
    /// </summary>
    /// <returns></returns>
    public static string[] GetSupportedVideoExtensions()
    {
      return [ ".MOV", ".MPG", ".AVI", ".MKV", ".MPEG", ".MP4" ];
    }

    /// <summary>
    /// Returns the Supported Image Extensions
    /// </summary>
    /// <returns></returns>
    public static string[] GetSupportedImageExtensions()
    {
      return [ ".JPG", ".JPEG", ".JFIF", ".PNG", ".BMP", ".GIF" ];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Path"></param>
    /// <returns></returns>
    public static PlaylistItemType GetPlayListItemTypeFromPath(string Path)
    {
      FileInfo info = new FileInfo(Path);
      
      if (GetSupportedImageExtensions().Contains(info.Extension.ToUpper()))
      {
        return PlaylistItemType.Image;
      }
      
      if (GetSupportedVideoExtensions().Contains(info.Extension.ToUpper()))
      {
        return PlaylistItemType.Video;
      }

      return PlaylistItemType.Invalid;
    }

    public static string GetRandomFileFromFolder(string fileName)
    {
      string Folder = new FileInfo(fileName).DirectoryName;
      Logger.LogComment("Getting random file from folder " + Folder);

      string[] files = Directory.GetFiles(Folder);
      Logger.LogComment("Found: " + files.Length + " files");

      int index = new Random((int)DateTime.Now.Ticks).Next(0, files.Length - 1);
      Logger.LogComment("Returning index: " + index);

      return files[index];
    }
  }
}
