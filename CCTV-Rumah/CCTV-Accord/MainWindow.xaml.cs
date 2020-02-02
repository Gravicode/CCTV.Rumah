using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

//using Ozeki.Media;
//using Ozeki.Camera;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
//using OpenCvSharp;
//using OpenCvSharp.Extensions;
using System.Net.Http;
using Newtonsoft.Json;
using System.Configuration;
//using Accord.Video.FFMPEG;
using System.Drawing;
using System.IO;
using CCTV_Accord.Helpers;
//using Emgu.CV;
//using Emgu.CV.Structure;

namespace CCTV_Accord
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public static bool IsGuardMode { set; get; } = false;
        #region Variables
        static bool IsPlaying { set; get; } = false;
        static EngineContainer ApiContainer = new EngineContainer();
        static HttpClient client = new HttpClient();
        static Dictionary<string, string> OldImages = new Dictionary<string, string>();
        static Timer CleanerTimer;
        //static List<string> JunkFiles = new List<string>();
        //static CascadeClassifier _localObjectDetector = new CascadeClassifier("Data/haarcascade_frontalface_alt2.xml");

        //head
        //static CascadeClassifier _localObjectDetector = new CascadeClassifier("Data/cascadeH5.xml");

        //human
        //static CascadeClassifier _localObjectDetector = new CascadeClassifier("Data/cascadG.xml");

        //head and shoulder
        //private static CascadeClassifier _localObjectDetector = new CascadeClassifier("Data/haarcascade_upperbody.xml");

        //static AzureBlobHelper BlobEngine = new AzureBlobHelper();

        //VideoViewerWPF[] videoViewer = new VideoViewerWPF[APPCONTANTS.CameraCount];


        static Dictionary<string, string> FrameId = new Dictionary<string, string>();
        #endregion
        static bool IsCleaning = false;
        #region Forms
        public MainWindow()
        {
            InitializeComponent();

            var DurationParams = ConfigurationManager.AppSettings["CheckDuration"].Split(':');

            //video cctv wpf
            //_drawingImageProvider[i] = new DrawingImageProvider();
            //videoViewer[i].SetImageProvider(_drawingImageProvider[i]);

            //frame for analysis

            //_frameHandler[i].SetInterval(new TimeSpan(int.Parse(DurationParams[0]), int.Parse(DurationParams[1]), int.Parse(DurationParams[2])));
             CleanerTimer =  new Timer((a)=> {
                 if (!IsCleaning)
                 {
                     IsCleaning = true;
                     LocalStorage.CleanTempFolder();
                     IsCleaning = false;
                 }
             },null,new TimeSpan(0,0,0), new TimeSpan(int.Parse(DurationParams[0]), int.Parse(DurationParams[1]), int.Parse(DurationParams[2])));
            
            //ApiContainer.Register<ComputerVisionService>(new ComputerVisionService());
            ApiContainer.Register<ObjectDetector>(new ObjectDetector());

            ConnectBtn.Click += ConnectBtn_Click;
            DisconnectBtn.Click += DisconnectBtn_Click;
            DisconnectBtn.IsEnabled = false;
            ChkGuard.Checked +=(a,b) => { IsGuardMode = ChkGuard.IsChecked.Value; };
            APPCONTANTS.CameraCount = int.Parse(ConfigurationManager.AppSettings["CameraCount"]);
            APPCONTANTS.CaptureTimeInterval = int.Parse(ConfigurationManager.AppSettings["CaptureTimeInterval"]);
            CleanTempBtn.Click += (a, b) => {
                if (IsCleaning)
                {
                    MessageBox.Show("Cleaning is in progress.");
                }
                else
                {
                    IsCleaning = true;
                    LocalStorage.CleanTempFolder();
                    IsCleaning = false;
                }
            };
            //_localObjectDetector.Load("Data/haarcascade_frontalface_alt2.xml");
            //_localObjectDetector.Load("Data/haarcascade_upperbody.xml");
            
            this.Closed +=(e,w)=>{
                LocalStorage.CleanTempFolder();
            };

        }
        private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {

            IsPlaying = true;
            ConnectBtn.IsEnabled = false;
            DisconnectBtn.IsEnabled = true;
            Task task1 = new Task(async()=> await DoPlaying());
            task1.Start();
        }

        /*
        private void SaveFrameButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Portable Network Graphics (*.png)|*.png";
            dialog.DefaultExt = "png";
            dialog.AddExtension = true;

            var result = dialog.ShowDialog();
            if (result.HasValue == false || result.Value == false) return;

            if (File.Exists(dialog.FileName))
                File.Delete(dialog.FileName);

            using (var fileStream = File.OpenWrite(dialog.FileName))
            {
                var encoder = new PngBitmapEncoder();
                var bitmap = videoViewer.GetCurrentFrame();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
        }*/

        private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            ConnectBtn.IsEnabled = true;
            DisconnectBtn.IsEnabled = false;
            IsPlaying = false;
        }
        #endregion

        #region Frame Processing
        private async Task DoPlaying()
        {
            var libDirectory = new DirectoryInfo(System.IO.Path.Combine(ConfigurationManager.AppSettings["VlcDir"], IntPtr.Size == 4 ? "x86" : "x64"));

            var options = new string[]
            {
                // VLC options can be given here. Please refer to the VLC command line documentation.
            };
            
            var reader = new List<Vlc.DotNet.Core.VlcMediaPlayer>();

            for (int i = 0; i < APPCONTANTS.CameraCount; i++)
            {
                var mediaPlayer = new Vlc.DotNet.Core.VlcMediaPlayer(libDirectory);
                mediaPlayer.SetMedia(new Uri(ConfigurationManager.AppSettings["cam" + (i + 1)]));
                /*
                mediaPlayer.PositionChanged += (sender, e) =>
                {
                    Console.Write("\r" + Math.Floor(e.NewPosition * 100) + "%");
                };
                */
                mediaPlayer.EncounteredError += (sender, e) =>
                {
                    Console.Error.Write("An error occurred");
                    //playFinished = true;
                };
                /*
                mediaPlayer.EndReached += (sender, e) => {
                    playFinished = true;
                };*/
                mediaPlayer.Play();
                reader.Add(mediaPlayer);
                /*
                var cam = new VideoFileReader();
                reader.Add(cam);
                cam.Open(ConfigurationManager.AppSettings["cam" + (i + 1)]);
                */
            }
            while (true)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    TxtDetect.Document.Blocks.Clear();
                }));
                //bool isProcessFrame = FrameCounter++ > FrameCounterLimit ? true : false;
                for (int i = 0; i < APPCONTANTS.CameraCount; i++)
                {
                    try
                    {
                        /*
                        if (!reader[i].IsOpen)
                        {
                            reader[i].Open(ConfigurationManager.AppSettings["cam" + (i + 1)]);
                        }*/

                        //Bitmap frame = reader[i].ReadVideoFrame((int)reader[i].FrameCount);
                        var tempFile = LocalStorage.GetTempFileName() + $"_cam{i}.jpg";
                        reader[i].TakeSnapshot(new FileInfo(tempFile));
                        if(File.Exists(tempFile))
                            await ProcessFrame(tempFile, $"cam{i + 1}");

                    }
                    catch
                    {
                        Console.WriteLine("failed to capture frame...");
                    }
                }
                
                    //Do whatever with the frame...
                 if (!IsPlaying) break;
                 Thread.Sleep(APPCONTANTS.CaptureTimeInterval);
            }
            for (int i = 0; i < APPCONTANTS.CameraCount; i++)
            {
                reader[i].Stop();
                reader[i].Dispose();
                //.Close();
            }
            //Debug.WriteLine("frame captured : " + e.DateStamp);
            //await ProcessFrame(e.ToImage(), FrameId[(sender as FrameCapture).ID]);


        }
        /*
        public BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            // bitmap.PixelFormat
            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }*/
        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
        public static BitmapSource Convert(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }
        /*
        public static Bitmap FindPeople(Image<Bgr, Byte> image, out List<System.Drawing.Rectangle> rects)
        {
            System.Drawing.Rectangle[] regions;
            //regions = _localObjectDetector.DetectMultiScale(image);

            //detect human
            //regions = _localObjectDetector.DetectMultiScale(image,scaleFactor:1.04,minNeighbors:4,minSize:new System.Drawing.Size(30,80),maxSize:new System.Drawing.Size(80,200));
            // human, 1.04, 4, 0 | 1, Size(30, 80), Size(80,200));

            //detect head
            regions = _localObjectDetector.DetectMultiScale(image, scaleFactor: 1.1, minNeighbors: 4, minSize: new System.Drawing.Size(20, 20), maxSize: new System.Drawing.Size(60, 60));
            //head, 1.1, 4, 0 | 1, Size(40, 40), Size(100, 100));

            //upper body
            //detect human
            //regions = _localObjectDetector.DetectMultiScale(image, scaleFactor: 1.05, minNeighbors: 4, minSize: new System.Drawing.Size(100, 68), maxSize: new System.Drawing.Size(200, 171));


            foreach (var rec in regions)
            {
                image.Draw(rec, new Bgr(System.Drawing.Color.Red), 2);
            }
            rects = new List<System.Drawing.Rectangle>(regions);
            return image.ToBitmap();
        }
        */
        /*
        public static Bitmap FindPeople(Image<Bgr, Byte> image, out List<System.Drawing.Rectangle> rects)
        {
            MCvObjectDetection[] regions;

            var Rects = new List<System.Drawing.Rectangle>();
            {  //this is the CPU version
                using (HOGDescriptor des = new HOGDescriptor())
                {
                    des.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
                    regions = des.DetectMultiScale(image, winStride : new System.Drawing.Size (4, 4),
                    padding : new System.Drawing.Size(8, 8), scale : 1.05);
                }
            }


            foreach (var pedestrain in regions)
            {
                var rec = pedestrain.Rect;
                Debug.WriteLine("detect : " + pedestrain.Score);
                image.Draw(rec, new Bgr(System.Drawing.Color.Red), 2);
                Rects.Add(rec);
            }
            rects = Rects;
            return image.ToBitmap();
        }
        */
        /*
        public static System.Drawing.Bitmap FindPeople(Bitmap bmp, out OpenCvSharp.Rect[] Rect)
        {
            Mat mat = bmp.ToMat();
            //var rects = _localObjectDetector.DetectMultiScale(image: mat, scaleFactor: 1.05, minNeighbors: 4, flags: HaarDetectionType.DoCannyPruning,
            //        minSize: new OpenCvSharp.Size(10, 10), maxSize: new OpenCvSharp.Size(60, 60));
            var rects = _localObjectDetector.DetectMultiScale(image: mat, scaleFactor: 1.05, minNeighbors: 4, flags: HaarDetectionType.DoCannyPruning,
                    minSize: new OpenCvSharp.Size(60, 60), maxSize: new OpenCvSharp.Size(200, 200));
            Rect = rects;
            foreach (var rec in rects)
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawRectangle(Pens.Red, new System.Drawing.Rectangle(rec.Left, rec.Top, rec.Width, rec.Height));
                }
            }
            return bmp;
        }*/
        async Task ProcessFrame(string FileImage, string CamName)
        {
            //System.Drawing.Image image,
            //var image = Bitmap.FromFile(FileImage);
            //string BlobName = CamName + DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss") + ".jpg";
            var res = await ApiContainer.GetApi<ObjectDetector>().ProcessFrame(FileImage,CamName);
            //Bitmap bmp = new Bitmap(Bitmap.FromFile(res.FileName)); //new Bitmap(image, new System.Drawing.Size(600, 337));            
            //emgu cv
            //List<System.Drawing.Rectangle> rects;
            //Image<Bgr, Byte> img = new Image<Bgr, byte>(bmp);
            //bmp = FindPeople(img, out rects);

            //opencv3
            //OpenCvSharp.Rect[] rects=null;
            //bmp = FindPeople(bmp, out rects);
            //call computer vision
            bool isPeopleExist = false;
            if (res.objects != null)
            {
                Debug.WriteLine($"object detected = {res.objects.Count}");

                if (res.objects.Count > 0)
                {
                    

                    //call computer vision

                    //bmp.Save("Photos/" + BlobName);

                    //var res = await ApiContainer.GetApi<ComputerVisionService>().RecognizeImage("Photos/" + BlobName);
                    //res = res.ToLower();

                    //bool PeopleExistx = false;
                    var result = "";
                    foreach (var item in res.objects)
                    {
                        if (item.Confidence > 0 && item.Label.Contains("person"))
                        {
                            isPeopleExist = true;
                        }
                        //if (item.Confidence>0.5 && item.Label.Contains("person"))
                        //{
                        //PeopleExistx = true;

                        //MemoryStream ms = new MemoryStream();
                        //bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                        //var IsUploaded = await BlobEngine.UploadFile(ms, BlobName);
                        //string UrlImg = "https://storagemurahaje.blob.core.windows.net/cctv/" + BlobName;
                        //if (IsUploaded)
                        //{
                        //    await PostToCloud(new CCTVData() { camName = CamName, description = res, imageUrl = UrlImg, tanggal = DateTime.Now });
                        //}
                        //}
                        result += $"{item.Label} = {item.Confidence.ToString("n2")},";
                    }
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {

                        var status = $"{CamName} - {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}: " + result;
                        TxtDetect.Document.Blocks.Add(new Paragraph(new Run(status)));
                    }));

                    //File.Delete("Photos/"+BlobName);
                }

            }
            if (!string.IsNullOrEmpty(res.FileName))
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                {

                    var img = new BitmapImage(new Uri(res.FileName));
                    switch (CamName)
                    {
                        case "cam1":
                            Cam1.Source = img;// BitmapToImageSource(bmp);
                        break;
                        case "cam2":
                            Cam2.Source = img;//BitmapToImageSource(bmp);
                        break;
                        case "cam3":
                            Cam3.Source = img;//BitmapToImageSource(bmp);
                        break;
                        case "cam4":
                            Cam4.Source = img;//BitmapToImageSource(bmp);
                        break;
                    }
                    /*
                    if (OldImages.ContainsKey(CamName))
                    {
                        if (!isPeopleExist)
                            JunkFiles.Add(OldImages[CamName]);
                        //File.Delete(OldImages[CamName]);
                    }*/
                    OldImages[CamName] = res.FileName;
                }));
            }
        }
        #endregion

        #region Cloud Processing
        async Task PostToCloud(CCTVData item)
        {
            string Url = "http://gravicodeabsensiweb.azurewebsites.net/api/CCTVs";
            var res = await client.PostAsync(Url, new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json"));
            if (res.IsSuccessStatusCode)
            {
                Debug.WriteLine("post to azure succeed");
            }
        }

        #endregion
    }

    public class CCTVData
    {
        public int id { get; set; }
        public string camName { get; set; }
        public string description { get; set; }
        public DateTime tanggal { get; set; }
        public string imageUrl { get; set; }
    }
}