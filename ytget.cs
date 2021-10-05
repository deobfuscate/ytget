using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ytget {
    class ytget {
        private static readonly HttpClient client = new HttpClient();
        static async System.Threading.Tasks.Task Main(string[] args) {
            Console.WriteLine("ytget v1.2");
            if (args == null || args.Length == 0) ShowHelp();
            Dictionary<string, string> video_data = new Dictionary<string, string>();
            string video_id = "", yt_page_data = "", content = "";
            JObject best = null;

            if (args[0].Contains("youtube.com") || args[0].Contains("youtu.be"))
                video_id = GetYouTubeId(args[0]);
            else {
                Console.WriteLine("ERROR: Invalid URL provided");
                Environment.Exit(-1);
            }
            try {
                yt_page_data = await client.GetStringAsync($"https://www.youtube.com/watch?v={video_id}");
            }
            catch {
                Console.WriteLine("ERROR: YouTube API could not be resolved");
                Environment.Exit(-2);
            }
            var search = new Regex("ytInitialPlayerResponse\\s*=\\s*(\\{.+?\\})\\s*;").Match(yt_page_data);

            if (!search.Success) {
                Console.WriteLine("ERROR: Could not find video meta data! (missing ytInitialPlayerResponse)");
                Environment.Exit(-3);
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
                Environment.Exit(-4);
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
                Environment.Exit(-5);
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
