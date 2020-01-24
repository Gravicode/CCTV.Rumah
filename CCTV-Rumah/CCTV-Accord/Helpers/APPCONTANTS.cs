using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCTV_Accord
{
   public  class APPCONTANTS
    {
        //cognitive service
        public const string MQTT_SERVER = "cloud.makestro.com";
        public const string MQTT_USER = "mifmasterz";
        public const string MQTT_PASS = "123qweasd";

        public const string BING_API_KEY = "7cbea11a8b5344f08b2b5db408f91ed4";
        public const string COMPUTERVISION_KEY = "1962967cccbe44ab943f94d5780c7b6a";
        public const string LUIS_APP_ID = "0aa11a64-a01f-40b1-afb6-2daffaabadc1";
        public const string LUIS_SUB_KEY = "0d97be5a9b63419b884977611ccdba1f";
        public const string EMOTION_KEY = "cc22aa743b0340cdb9fb9094cdaadf53";
        public const string FACE_KEY = "0b06681714024cadbd5adcd3d00a40c1";
        public const string TEXTANALYSIS_KEY = "d850796f81484952a3fe3c6bfcaac5ba";

        public static readonly string BlobConnString = ConfigurationManager.AppSettings["BlobConn"];
        public static readonly string BlobContainerName = ConfigurationManager.AppSettings["BlobContainerName"];

        public static int CameraCount = 3;
        public static int CaptureTimeInterval = 3000;
    }
}
