using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ytget {
    class YTGet {
        private const int ERR_INVALID_URL = -1, ERR_API_UNRESOLVED = -2, ERR_NO_METADATA = -3, ERR_NO_EMBEDDING = -4, ERR_DOWNLOAD_FAILED = -5;
        private const string PATTERN = "ytInitialPlayerResponse\\s*=\\s*(\\{.+?\\})\\s*;";
        private static readonly HttpClient client = new HttpClient();

        static async System.Threading.Tasks.Task Main(string[] args) {
            Console.WriteLine("ytget v1.2");
            if (args == null || args.Length == 0)
                ShowHelp();
            Dictionary<string, string> videoData = new Dictionary<string, string>();
            string videoId = "", pageData = "", content = "";
            JObject best = null;
            Match search = null;

            if (GetYouTubeId(args[0]) != null)
                videoId = GetYouTubeId(args[0]);
            else
                Error("Invalid URL provided", ERR_INVALID_URL);
            try {
                pageData = await client.GetStringAsync($"https://www.youtube.com/watch?v={videoId}");
            }
            catch {
                Error("YouTube video url could not be resolved", ERR_API_UNRESOLVED);
            }
            search = new Regex(PATTERN).Match(pageData);

            if (!search.Success) 
                Error("Could not find video metadata! (missing ytInitialPlayerResponse)", ERR_NO_METADATA);
            content = search.Result("$1");
            #if DEBUG
            File.Delete("player_response.txt");
            File.AppendAllText("player_response.txt", content);
            #endif
            dynamic decodedObj = JObject.Parse(content);
            Console.WriteLine("Video Title: " + decodedObj["videoDetails"]["title"]);
            if (decodedObj["streamingData"] == null)
                Error("Failed to download, the video has disabled embedding", ERR_NO_EMBEDDING);
            foreach (var video in decodedObj["streamingData"]["formats"]) {
                if (best == null || video["bitrate"] > best["bitrate"])
                    best = video;
            }
            if (!string.IsNullOrEmpty(best["qualityLabel"].ToString()))
                Console.WriteLine($"Found video! Downloading highest quality ({best["qualityLabel"]})...");
            else
                Console.WriteLine("Found video! Downloading highest quality...");
            try {
                new WebClient().DownloadFile(best["url"].ToString(),
                    RemoveInvalidChars($"{decodedObj["videoDetails"]["title"]} - {decodedObj["videoDetails"]["videoId"]}.mp4"));
            }
            catch {
                Error("Failed to download video", ERR_DOWNLOAD_FAILED);
            }
            Console.WriteLine("Download done!");
        }

        static string GetYouTubeId(string url) {
            if (url.Contains("youtube.com"))
                return url.Split('=')[1];
            else if (url.Contains("youtu.be"))
                return url.Split('/')[3];
            else
                return null;
        }

        static string RemoveInvalidChars(string filename) => string.Concat(filename.Split(Path.GetInvalidFileNameChars()));

        static void Error(string message, int errorCode) {
            Console.WriteLine($"ERROR: {message}");
            Environment.Exit(errorCode);
        }

        static void ShowHelp() {
            Console.WriteLine("Usage: ytget.exe <link to youtube video>");
            Console.WriteLine("Example: ytget.exe https://www.youtube.com/watch?v=jNQXAC9IVRw");
            Environment.Exit(0);
        }
    }
}
