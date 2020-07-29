using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows.Forms;

namespace ytget
{
    class ytget
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
            Console.WriteLine("ytget v1.0");
            if (args == null || args.Length == 0) ShowHelp();
            Dictionary<string, string> video_data = new Dictionary<string, string>();
            string video_id = "", raw_data = "";
            JObject best = null;

            if (args[0].Contains("youtube.com") || args[0].Contains("youtu.be"))
                video_id = GetYouTubeId(args[0]);
            else
            {
                Console.WriteLine("ERROR: Invalid URL provided");
                Environment.Exit(-1);
            }
            try
            {
                raw_data = new WebClient().DownloadString($"https://www.youtube.com/get_video_info?video_id={video_id}");
            }
            catch
            {
                Console.WriteLine("ERROR: Failed to connect to YouTube");
                Environment.Exit(-1);
            }
            string[] video_data_vals = raw_data.Split('&');
#if DEBUG
            File.Delete("output.txt");
            foreach (string pair in video_data_vals)
                File.AppendAllText("output.txt", pair + "\n\n");
#endif
            foreach (string item in video_data_vals)
            {
                string[] tmp = item.Split('=');
                video_data.Add(tmp[0], tmp[1]);
            }
            if (!video_data.ContainsKey("player_response"))
            {
                Console.WriteLine("ERROR: Could not find any videos! (missing player_response)");
                Environment.Exit(-1);
            }
            string decoded_str = WebUtility.UrlDecode(video_data["player_response"]);
#if DEBUG
            File.WriteAllText("player_response.txt", decoded_str);
#endif
            dynamic decoded_obj = JObject.Parse(decoded_str);
            Console.WriteLine("Video Title: " + decoded_obj["videoDetails"]["title"]);
            if (decoded_obj["streamingData"] == null)
            {
                Console.WriteLine("ERROR: Failed to download, the video has disabled embedding");
                Environment.Exit(-1);
            }
            foreach (var video in decoded_obj["streamingData"]["formats"])
            {
                if (best == null) best = video;
                else if (video["bitrate"] > best["bitrate"])
                    best = video;
            }
            Console.WriteLine("Found video! Downloading highest quality...");
            try
            {
                new WebClient().DownloadFile(best["url"].ToString(), 
                    RemoveInvalidChars($"{decoded_obj["videoDetails"]["title"]} - {decoded_obj["videoDetails"]["videoId"]}.mp4"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Failed to download video");
                Environment.Exit(-1);
            }
            Console.WriteLine("Download done!");
        }

        static string GetYouTubeId(string url)
        {
            if (url.Contains("youtube.com"))
                return url.Split('=')[1];
            else if (url.Contains("youtu.be"))
                return url.Split('/')[3];
            else
                return "";
        }

        static string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage: ytget.exe <link to youtube video>");
            Console.WriteLine("Example: ytget.exe https://www.youtube.com/watch?v=jNQXAC9IVRw");
            Environment.Exit(0);
        }
    }
}
