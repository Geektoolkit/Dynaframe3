using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dynaframe3
{
    public class HttpSettings
    {
        private string _urls = "";
        public string urls
        {
            get => _urls;
            set
            {
                _urls = value;
                Endpoints = _urls.Split(';').Select(u => new Endpoint(u));
            }
        }

        public IEnumerable<Endpoint> Endpoints { get; private set; }
    }

    public class Endpoint
    {
        // We have to use this regex instead of the Uri class cause you can get things like 'http://*:8080' which
        // Uri considers invalid.
        private static Regex _parseRegex = new Regex(@"(http|https)://([\w+.?|*]+):(\d+)", RegexOptions.Compiled);

        private readonly Match _match;

        public string Url { get; }

        public string Scheme => _match.Groups[1].Value;

        public string Host => _match.Groups[2].Value;

        public string ExternalHost
        {
            get
            {
                switch (Host.ToLower())
                {
                    case "localhost":
#if DEBUG
                        return "localhost";
#else
                        throw new InvalidOperationException("External host cannot be set to localhost. Must be either a DNS name or IP Address");
#endif
                    case "*":
                        return Helpers.GetIP();
                    default:
                        return Host;
                }
            }
        }

        public int Port => int.Parse(_match.Groups[3].Value);

        public Endpoint(string url)
        {
            Url = url;

            _match = _parseRegex.Match(url);

            if (!_match.Success)
            {
                throw new InvalidOperationException($"Failed to parse url: {url}");
            }
        }
    }
}
