using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ytget {
    class YTGet {
        private const int SUCCESS = 0,
            ERR_INVALID_URL = -1,
            ERR_API_UNRESOLVED = -2,
            ERR_NO_METADATA = -3,
            ERR_DOWNLOAD_FAILED = -4,
            ERR_NOT_SUPPORTED = -5;
        private const string PATTERN = "ytInitialPlayerResponse\\s*=\\s*(\\{.+?\\})\\s*;";
        private static readonly HttpClient client = new HttpClient();

        static async System.Threading.Tasks.Task Main(string[] args) {
            Console.WriteLine("ytget v1.2");
            if (args == null || args.Length == 0) {
                ShowHelp();
            }
            Dictionary<string, string> videoData = new Dictionary<string, string>();
            string videoId = "", pageData = "", content = "";
            JObject best = null;
            Match search = null;

            if (GetYouTubeId(args[0]) != null) {
                videoId = GetYouTubeId(args[0]);
            }
            else {
                Error("Invalid URL provided", ERR_INVALID_URL);
            }

            try {
                pageData = await client.GetStringAsync($"https://www.youtube.com/watch?v={videoId}");
            }
            catch {
                Error("YouTube video url could not be resolved", ERR_API_UNRESOLVED);
            }

            search = new Regex(PATTERN).Match(pageData);
            if (!search.Success) {
                Error("Could not find video metadata! (missing ytInitialPlayerResponse)", ERR_NO_METADATA);
            }
            content = search.Result("$1");

            #if DEBUG
                File.WriteAllText("player_response.txt", content);
            #endif

            dynamic decodedObj = JObject.Parse(content);
            Console.WriteLine("Video Title: " + decodedObj["videoDetails"]["title"]);
            if (decodedObj["streamingData"] == null || decodedObj["streamingData"]["formats"] == null) {
                Error("Failed to download, the video has disabled embedding", ERR_NOT_SUPPORTED);
            }
            foreach (var video in decodedObj["streamingData"]["formats"]) {
                if (best == null || video["bitrate"] > best["bitrate"]) {
                    best = video;
                }
            }
            Console.WriteLine($"Found video! Downloading highest quality{(!string.IsNullOrEmpty(best["qualityLabel"].ToString()) ? $" ({best["qualityLabel"]})" : "")}...");
            try {
                var wc = new WebClient();
                wc.DownloadFile(best["url"].ToString(),
                    RemoveInvalidChars($"{decodedObj["videoDetails"]["title"]} - {decodedObj["videoDetails"]["videoId"]}.mp4"));
                wc.DownloadProgressChanged += (s, e) => {
                    Console.WriteLine(e.ProgressPercentage);
                };
            }
            catch {
                Error("Failed to download video", ERR_DOWNLOAD_FAILED);
            }
            Console.WriteLine("Download done!");
        }

        static string GetYouTubeId(string url) {
            if (url.Contains("youtube.com")) {
                return url.Split('=')[1];
            }
            else if (url.Contains("youtu.be")) {
                return url.Split('/')[3];
            }
            else {
                return null;
            }
        }

        static string RemoveInvalidChars(string filename) => string.Concat(filename.Split(Path.GetInvalidFileNameChars()));

        static void Error(string message, int errorCode) {
            Console.WriteLine($"ERROR: {message}");
            Environment.Exit(errorCode);
        }

        static void ShowHelp() {
            Console.WriteLine("Usage: ytget.exe <link to youtube video>");
            Console.WriteLine("Example: ytget.exe https://www.youtube.com/watch?v=jNQXAC9IVRw");
            Environment.Exit(SUCCESS);
        }
    }
}