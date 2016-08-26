using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace MunichTimelapse
{
    internal class UploadVideo
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Main Timelapse creation routine (running until 00:00)");
            Console.WriteLine("==============================");
            tlCreate();

            Console.WriteLine("YouTube Data API: Upload Video (copypasted)");
            Console.WriteLine("==============================");
            try
            {
                new UploadVideo().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }

        private async Task Run()
        {
            UserCredential credential;
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows an application to upload files to the
                    // authenticated user's YouTube channel, but doesn't allow other types of access.
                    new[] { YouTubeService.Scope.YoutubeUpload },
                    "user",
                    CancellationToken.None
                );
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
            });

            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = "Marienplatz (Munich) Timelapse " + DateTime.Now.Date.AddDays(-1).ToString("dd.MM.yyyy"); ;
            video.Snippet.Description = @"Webcam used: http://stories.ludwigbeck.de/webcam";
            video.Snippet.Tags = new string[] { "Marienplatz", "Timelapse", "Munich", "Munchen", "Webcam", DateTime.Now.Date.AddDays(-1).ToString("yyyyMMdd") };
            video.Snippet.CategoryId = "22"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
            video.Status = new VideoStatus();
            video.Status.PrivacyStatus = "unlisted"; // or "private" or "public"
            var filePath = DateTime.Now.Date.AddDays(-1).ToString("yyyyMMdd") + @"\" + DateTime.Now.Date.AddDays(-1).ToString("yyyyMMdd") + ".mp4";

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += uploadProgressChanged;
                videosInsertRequest.ResponseReceived += uploadResponseReceived;

                await videosInsertRequest.UploadAsync();
            }
        }

        void uploadProgressChanged(IUploadProgress progress)
        {
            Console.WriteLine(progress.Status + " " + progress.BytesSent);
        }
        void uploadResponseReceived(Video video)
        {
            Console.WriteLine("Video id '{0}' was successfully uploaded.", video.Id);
        }

        static void tlCreate()
        {
            //init 
            int today = DateTime.Now.Day;
            int count = 0;

            //create a new folder
            string newFolder = Convert.ToString(AppDomain.CurrentDomain.BaseDirectory) + "" + DateTime.Now.Date.ToString("yyyyMMdd") + @"\";
            Directory.CreateDirectory(newFolder);

            //check if there is anything in that folder
            var jpgFiles = Directory.GetFiles(newFolder, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine();
            if (jpgFiles.Count() > 0)
            {
                Console.WriteLine("[{0}] I've found {1} files in today's folder", DateTime.Now.ToString(), jpgFiles.Count());

                //retrieving last file's name
                string lastFilePath = jpgFiles.ElementAt((jpgFiles.Count()) - 1);
                string folderNameTrim = lastFilePath.Substring(lastFilePath.Length - 10); //folder path trim
                string lastFileName = folderNameTrim.Remove(folderNameTrim.Length - 4); //extension trim

                Console.WriteLine("[{0}] Last number is {1}", DateTime.Now.ToString(), Convert.ToInt32(lastFileName));
                count = Convert.ToInt32(lastFileName) + 1;
            }

/*            while (DateTime.Now.Day == today)
            {
                //save image
                Console.WriteLine("[{0}] Saving image {1}", DateTime.Now.ToString(), "img" + count.ToString("D6") + ".jpg");
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile("http://kaufhaus.ludwigbeck.de/manual/webcam/1sec.jpg", newFolder + @"\img" + count.ToString("D6") + ".jpg");
                }
                Thread.Sleep(5000);
                count++;
            }
            Console.WriteLine("[{0}] Day is over", DateTime.Now.ToString());
*/
            convert(newFolder);
        }

        static void convert(string newFolder)
        {
            //check if we've already copied ffmpeg.exe
            var ffmpegFile = Directory.GetFiles(newFolder, "ffmpeg.exe", SearchOption.AllDirectories);
            if (ffmpegFile.Count() == 0)
            {
                File.Copy(Convert.ToString(AppDomain.CurrentDomain.BaseDirectory) + "ffmpeg.exe", newFolder + "ffmpeg.exe");
            }

            //run ffmpeg
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.Arguments = @"-r 15 -f image2 -s 1024x768 -start_number 0 -i img%06d.jpg -vcodec libx264 -crf 15 -pix_fmt yuv420p " + newFolder.Substring(newFolder.Length - 9).Replace(@"\", "") + ".mp4";
            p.StartInfo.FileName = "ffmpeg.exe";
            p.StartInfo.WorkingDirectory = newFolder;
            p.OutputDataReceived += (sender, args) => Console.WriteLine("{0}", args.Data);
            p.Start();
            p.WaitForExit();

/*            //cleanup
            Console.WriteLine("[{0}] Cleanup", DateTime.Now.ToString());
            File.Delete(newFolder + "ffmpeg.exe");
            var jpgCleanup = Directory.GetFiles(newFolder, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
            for (int i = 0; i < jpgCleanup.Count(); i++)
            {
                File.Delete(jpgCleanup.ElementAt(i));
            }
*/        }
    }
}