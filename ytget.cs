using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ytget {
    class ytget {
        private const int ERR_INVALID_URL = -1;
        private const int ERR_API_UNRESOLVED = -2;
        private const int ERR_NO_METADATA = -3;
        private const int ERR_NO_EMBEDDING = -4;
        private const int ERR_DOWNLOAD_FAILED = -5;
        private const string PATTERN = "ytInitialPlayerResponse\\s*=\\s*(\\{.+?\\})\\s*;";
        private static readonly HttpClient client = new HttpClient();

        static async System.Threading.Tasks.Task Main(string[] args) {
            Console.WriteLine("ytget v1.2");
            if (args == null || args.Length == 0)
                ShowHelp();
            Dictionary<string, string> video_data = new Dictionary<string, string>();
            string video_id = "", yt_page_data = "", content = "";
            JObject best = null;
            Match search = null;

            if (args[0].Contains("youtube.com") || args[0].Contains("youtu.be"))
                video_id = GetYouTubeId(args[0]);
            else {
                Console.WriteLine("ERROR: Invalid URL provided");
                Environment.Exit(ERR_INVALID_URL);
            }
            try {
                yt_page_data = await client.GetStringAsync($"https://www.youtube.com/watch?v={video_id}");
            }
            catch {
                Console.WriteLine("ERROR: YouTube video url could not be resolved");
                Environment.Exit(ERR_API_UNRESOLVED);
            }
            search = new Regex(PATTERN).Match(yt_page_data);

            if (!search.Success) {
                Console.WriteLine("ERROR: Could not find video metadata! (missing ytInitialPlayerResponse)");
                Environment.Exit(ERR_NO_METADATA);
            }
            content = search.Result("$1");
            #if DEBUG
            File.Delete("player_response.txt");
            File.AppendAllText("player_response.txt", content);
            #endif
            dynamic decoded_obj = JObject.Parse(content);
            Console.WriteLine("Video Title: " + decoded_obj["videoDetails"]["title"]);
            if (decoded_obj["streamingData"] == null) {
                Console.WriteLine("ERROR: Failed to download, the video has disabled embedding");
                Environment.Exit(ERR_NO_EMBEDDING);
            }
            foreach (var video in decoded_obj["streamingData"]["formats"]) {
                if (best == null)
                    best = video;
                else if (video["bitrate"] > best["bitrate"])
                    best = video;
            }
            Console.WriteLine("Found video! Downloading highest quality...");
            try {
                new WebClient().DownloadFile(best["url"].ToString(), 
                    RemoveInvalidChars($"{decoded_obj["videoDetails"]["title"]} - {decoded_obj["videoDetails"]["videoId"]}.mp4"));
            }
            catch {
                Console.WriteLine("ERROR: Failed to download video");
                Environment.Exit(ERR_DOWNLOAD_FAILED);
            }
            Console.WriteLine("Download done!");
        }

        static string GetYouTubeId(string url) {
            if (url.Contains("youtube.com"))
                return url.Split('=')[1];
            else if (url.Contains("youtu.be"))
                return url.Split('/')[3];
            else
                return "";
        }

        static string RemoveInvalidChars(string filename) => string.Concat(filename.Split(Path.GetInvalidFileNameChars()));

        static void ShowHelp() {
            Console.WriteLine("Usage: ytget.exe <link to youtube video>");
            Console.WriteLine("Example: ytget.exe https://www.youtube.com/watch?v=jNQXAC9IVRw");
            Environment.Exit(0);
        }
    }
}
