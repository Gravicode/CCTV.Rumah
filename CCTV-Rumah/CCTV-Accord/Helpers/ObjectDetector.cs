using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Microsoft.ML;
using System.Threading.Tasks;
using Microsoft.ML.Data;
using ObjectDetection.DataStructures;
using ObjectDetection.YoloParser;
using ObjectDetection;

namespace CCTV_Accord.Helpers
{
    public class ObjectDetector
    {
        //static string assetsRelativePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
        static string assetsPath = GetAbsolutePath("");
        static string modelFilePath = Path.Combine(assetsPath, "Models", "TinyYolo2_model.onnx");
        static string imagesFolder = Path.Combine(assetsPath, "images");
        public static string outputFolder = Path.Combine(assetsPath, "images", "output");
        static OnnxModelScorer modelScorer;
        // Initialize MLContext
        MLContext mlContext = new MLContext();
        public async Task<(IList<YoloBoundingBox> objects, string FileName)> ProcessFrame(string ImageFileInput, string CamName)
        {
            var outputFile = "";
            var res = await Task.Run<IList<YoloBoundingBox>>(() =>
            {
                try
                {
                    // Load Data
                    //IEnumerable<ImageNetData> images = ImageNetData.ReadFromFile(imagesFolder);
                    //var tempFile = Path.GetTempFileName() + $"_{CamName}.jpg";
                    //image.Save(tempFile);

                    IEnumerable<ImageNetData> images = new List<ImageNetData> { new ImageNetData() { ImagePath = ImageFileInput, Label = Path.GetFileName(ImageFileInput) } };
                    IDataView imageDataView = mlContext.Data.LoadFromEnumerable(images);

                    // Create instance of model scorer
                    if (modelScorer == null) modelScorer = new OnnxModelScorer(imagesFolder, modelFilePath, mlContext);

                    // Use model to score data
                    IEnumerable<float[]> probabilities = modelScorer.Score(imageDataView);

                    // Post-process model output
                    YoloOutputParser parser = new YoloOutputParser();

                    var boundingBoxes =
                            probabilities
                            .Select(probability => parser.ParseOutputs(probability))
                            .Select(boxes => parser.FilterBoundingBoxes(boxes, 5, .5F));

                    // Draw bounding boxes for detected objects in each of the images
                    for (var i = 0; i < images.Count(); i++)
                    {
                        string imageFileName = images.ElementAt(i).ImagePath;
                        IList<YoloBoundingBox> detectedObjects = boundingBoxes.ElementAt(i);

                        outputFile = DrawBoundingBox(outputFolder, imageFileName, detectedObjects,CamName);

                        LogDetectedObjects(imageFileName, detectedObjects);
                        return detectedObjects;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                return null;
            });

            return (res, outputFile);

        }
        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(ObjectDetector).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }

        private static string DrawBoundingBox(string outputImageLocation, string imageFileName, IList<YoloBoundingBox> filteredBoundingBoxes,string CamName)
        {
            Image image = Image.FromFile(imageFileName);

            var originalImageHeight = image.Height;
            var originalImageWidth = image.Width;

            foreach (var box in filteredBoundingBoxes)
            {
                // Get Bounding Box Dimensions
                var x = (uint)Math.Max(box.Dimensions.X, 0);
                var y = (uint)Math.Max(box.Dimensions.Y, 0);
                var width = (uint)Math.Min(originalImageWidth - x, box.Dimensions.Width);
                var height = (uint)Math.Min(originalImageHeight - y, box.Dimensions.Height);

                // Resize To Image
                x = (uint)originalImageWidth * x / OnnxModelScorer.ImageNetSettings.imageWidth;
                y = (uint)originalImageHeight * y / OnnxModelScorer.ImageNetSettings.imageHeight;
                width = (uint)originalImageWidth * width / OnnxModelScorer.ImageNetSettings.imageWidth;
                height = (uint)originalImageHeight * height / OnnxModelScorer.ImageNetSettings.imageHeight;

                // Bounding Box Text
                string text = $"{box.Label} ({(box.Confidence * 100).ToString("0")}%)";

                using (Graphics thumbnailGraphic = Graphics.FromImage(image))
                {
                    thumbnailGraphic.CompositingQuality = CompositingQuality.HighQuality;
                    thumbnailGraphic.SmoothingMode = SmoothingMode.HighQuality;
                    thumbnailGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // Define Text Options
                    Font drawFont = new Font("Arial", 12, FontStyle.Bold);
                    SizeF size = thumbnailGraphic.MeasureString(text, drawFont);
                    SolidBrush fontBrush = new SolidBrush(Color.Black);
                    Point atPoint = new Point((int)x, (int)y - (int)size.Height - 1);

                    // Define BoundingBox options
                    Pen pen = new Pen(box.BoxColor, 3.2f);
                    SolidBrush colorBrush = new SolidBrush(box.BoxColor);

                    // Draw text on image 
                    thumbnailGraphic.FillRectangle(colorBrush, (int)x, (int)(y - size.Height - 1), (int)size.Width, (int)size.Height);
                    thumbnailGraphic.DrawString(text, drawFont, fontBrush, atPoint);

                    // Draw bounding box on image
                    thumbnailGraphic.DrawRectangle(pen, x, y, width, height);
                }
            }

            if (!Directory.Exists(outputImageLocation))
            {
                Directory.CreateDirectory(outputImageLocation);
            }
            //save only frame that contains person
            bool PeopleExist = false;
            
            foreach (var item in filteredBoundingBoxes)
            {
                if (item.Confidence > 0 && item.Label.Contains("person"))
                {
                    PeopleExist = true;
                    break;
                }
            }
            var OutputName = PeopleExist && MainWindow.IsGuardMode? System.IO.Path.Combine(ObjectDetector.outputFolder, $"{DateTime.Now.ToString("yyyyMMdd_HHmmss")}_{CamName}.jpg"):Path.GetTempFileName()+".jpg";
            
            image.Save(OutputName);
            image.Dispose();
            File.Delete(imageFileName);
            return OutputName;
        }

        private static void LogDetectedObjects(string imageName, IList<YoloBoundingBox> boundingBoxes)
        {
            Console.WriteLine($".....The objects in the image {imageName} are detected as below....");

            foreach (var box in boundingBoxes)
            {
                Console.WriteLine($"{box.Label} and its Confidence score: {box.Confidence}");
            }

            Console.WriteLine("");
        }
    }
}
