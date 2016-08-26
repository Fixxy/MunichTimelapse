using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MunichTimelapse
{
    class Program
    {
        static void Main(string[] args)
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
                string folderNameTrim = lastFilePath.Substring(lastFilePath.Length - 10); // folder path trim
                string lastFileName = folderNameTrim.Remove(folderNameTrim.Length - 4); //extension trim

                Console.WriteLine("[{0}] Last number is {1}", DateTime.Now.ToString(), Convert.ToInt32(lastFileName));
                count = Convert.ToInt32(lastFileName) + 1;
            }

            while (DateTime.Now.Day == today)
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

            //cleanup
            Console.WriteLine("[{0}] Cleanup", DateTime.Now.ToString());
            File.Delete(newFolder + "ffmpeg.exe");
            var jpgCleanup = Directory.GetFiles(newFolder, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
            for (int i = 0; i < jpgCleanup.Count(); i++)
            {
                File.Delete(jpgCleanup.ElementAt(i));
            }
        }
    }
}