using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dynaframe3
{
    public static  class PlayListEngineHelper
    {
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
            return new string[] { ".MOV", ".MPG", ".AVI", ".MKV", ".MPEG", ".MP4" };
        }
        /// <summary>
        /// Returns the Supported Image Extensions
        /// </summary>
        /// <returns></returns>
        public static string[] GetSupportedImageExtensions()
        {
            return new string[] { ".JPG", ".JPEG", ".PNG", ".BMP" };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        public static PlayListItemType GetPlayListItemTypeFromPath(string Path)
        {
            FileInfo info = new FileInfo(Path);
            if (GetSupportedImageExtensions().Contains(info.Extension.ToUpper()))
            {
                return PlayListItemType.Image;
            }
            if (GetSupportedVideoExtensions().Contains(info.Extension.ToUpper()))
            {
                return PlayListItemType.Video;
            }
            return PlayListItemType.Invalid;
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
