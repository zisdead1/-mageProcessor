#pragma warning disable 4014  //This code has an asynchronus event handler

using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using OpenCvSharp;//nuget OpenCVSharp4 package
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace ÌmageProcessor
{
    class ÌmageProcessor
    {
        #region Members

        struct Image
        {
            public Mat mat;//openCV image matrix 
            public string filename;
        }

        //Simple log file to prove we have out of order async image processing
        public static FileStream fs = File.Create(@"C:\Logs\ÌmageProcessor\Log.txt");

        #endregion

        #region Methods

        private static void Main(string[] args)
        {
            //first time around the Output directory for converted
            //images will not exist
            string outputdir = @"C:\Temp\MetroImgs\Output";
            if (!Directory.Exists(outputdir))
            {
                Directory.CreateDirectory(outputdir);
            }

            string logdir = @"C:\Logs\ÌmageProcessor";
            // If directory does not exist, create it
            if (!Directory.Exists(logdir))
            {
                Directory.CreateDirectory(logdir);
            }

            Console.WriteLine("Starting Image processing");
            WriteToLog("Starting Image processing \n");
            StartProducerConsumer();
            
            Console.ReadKey();
        }

        private static void StartProducerConsumer()
        {
            var imageQueue = new BufferBlock<Image>();
            FetchRawImages(imageQueue);
            AsynchronousImageConv(imageQueue);
        }

        private static async void FetchRawImages(ITargetBlock<Image> imageQueue)
        {
            await Task.Run(
            () =>
            {
                //assume that this directory is created and has files for processing
                var filePaths = Directory.GetFiles(@"C:\Temp\MetroImgs", "*");
                foreach (var path in filePaths)
                {
                    Image image = new Image();
                    image.filename = Path.GetFileName(path);
                    image.mat = Cv2.ImRead(path);
                    imageQueue.Post(image);
                    Console.WriteLine("Image Captured at {0} and placed in queue", Path.GetFileName(path));
                    WriteToLog("Image Captured at " + Path.GetFileName(path) + " and placed in queue \n");
                    Thread.Sleep(300);
                }
            });
        }

        //This is the method that consumes the Matrix images and runs some openCV 
        //processing on each before saving them. The threads are asynchronus 
        //so processing can occur out of order intentionally to speed up the flow
        private static async void AsynchronousImageConv(ISourceBlock<Image> imageQueue)
        {
            while (await imageQueue.OutputAvailableAsync())
            {
                Image producedResult = imageQueue.Receive(); 
             
                
                Task.Run(
                 () =>
                     {
                         if (DateTime.Now.Ticks % 3 == 0)
                         {
                             Thread.Sleep(550);
                         }

                         Mat output = new Mat();
                         Mat input = producedResult.mat;
                         Mat flipped = input.Flip(FlipMode.Y);
                         //create an inversion effect i.e white becomes black
                         Cv2.BitwiseNot(flipped, output);
                         string outputName = @"C:\Temp\MetroImgs\Output\" + producedResult.filename;
                         output.ImWrite(outputName);
                         Console.WriteLine("Processed Image {0} from the queue:", producedResult.filename);
                         WriteToLog("Processed Image " + producedResult.filename + " from the queue: \n");
                     });
            }
        }

        private static void WriteToLog(string logline)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(logline);
            fs.Write(info, 0, info.Length);
        }

        #endregion
    }
}

