using Dynaframe3.Models;
using Dynaframe3.Shared;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dynaframe3.Playlist
{
  public class Engine
  {
    private int _mediaIndex = 0;

    public MediaFile CurrentMediaFile { get; set; }
    
    public int MediaIndex { 
      get => _mediaIndex; 
      private set => _mediaIndex = value; 
    }

    public List<int> Playlist;

    public Engine() {

      Playlist = new List<int>();
    }

    public void GoToFirstFile() {
      MediaIndex = 0;
    }

    public void RebuildPlaylist(AppSettings appsettings) {
      Stopwatch sw = new Stopwatch();
      sw.Start();

    }
  }
}
