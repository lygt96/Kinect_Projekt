using System;
using Unosquare.RaspberryIO;
using LCD_Displays;
using freenect;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Servo_Kinect
{
    class Program
    {

        public Thread updateThread = null;

        public static void Main()
        {
            int LCD_RS = 12;
            int LCD_E = 16;
            int LCD_DATA4 = 6;
            int LCD_DATA5 = 13;
            int LCD_DATA6 = 19;
            int LCD_DATA7 = 26;
            Thread updateThread = null;

            Console.WriteLine("Debug Hook v3");
            Console.ReadKey();

            Console.WriteLine("Initialisiere LCD");
            LCD LCD = new LCD(LCD_RS, LCD_E, LCD_DATA4, LCD_DATA5, LCD_DATA6, LCD_DATA7);
            LCD.InitializeLCD();

            /*
            Console.WriteLine("Teste EmguCV");
            String win1 = "Test Window"; //The name of the window
            CvInvoke.NamedWindow(win1); //Create the window using the specific name

            Mat img = new Mat(200, 400, DepthType.Cv8U, 3); //Create a 3 channel image of 400x200
            img.SetTo(new Bgr(255, 0, 0).MCvScalar); // set it to Blue color

            //Draw "Hello, world." on the image using the specific font
            CvInvoke.PutText(
                img,
                "Hello Bibi",
                new System.Drawing.Point(10, 80),
                FontFace.HersheyComplex,
                1.0,
                new Bgr(0, 0, 0).MCvScalar);

            CvInvoke.Imshow(win1, img); //Show the image
            CvInvoke.DestroyWindow(win1); //Destroy the window if key is pressed
            Console.WriteLine("EmguCV Test abgeschlossen.");*/

            Console.WriteLine("Initialisiere Kinect.");
            Kinect kinect = new Kinect(0);    
            Init(kinect);
            Console.WriteLine("Kinect initialisiert.");

            Console.WriteLine("Soll ein Servotest durchgeführt werden? [y/n]");
            if(Console.ReadKey().KeyChar.Equals("y"))
            {
                Console.WriteLine("Servotest wird ausgeführt.");
                ServoTest(kinect);
            }
            else
            {
                Console.WriteLine("Servotest wird übersprungen.");
            }

            Console.WriteLine("In welchem Modus soll die Tiefenkamera arbeiten?");
            for (int i = 0; i < kinect.DepthCamera.Modes.Length; i++)
            {
                Console.WriteLine(i + " - " + kinect.DepthCamera.Modes[i]);
            }
            var input_tiefe = Convert.ToInt32(Console.ReadLine());
            kinect.DepthCamera.Mode = kinect.DepthCamera.Modes[input_tiefe];
            Console.WriteLine("Tiefenkamera arbeitet im Modus " + kinect.DepthCamera.Mode);


            Console.WriteLine("In welchem Modus soll die Farbkamera arbeiten?");
            for (int i = 0; i < kinect.VideoCamera.Modes.Length; i++)
            {
                Console.WriteLine(i + " - " + kinect.VideoCamera.Modes[i]);
            }
            var input_rgb = Convert.ToInt32(Console.ReadLine());
            kinect.VideoCamera.Mode = kinect.VideoCamera.Modes[input_rgb];
            Console.WriteLine("Farbkamera arbeitet im Modus " + kinect.VideoCamera.Mode);

            Console.WriteLine("Erstelle Thread zum Updaten des Kinects.");
            updateThread = new Thread(delegate ()
            {
                try
                {
                    kinect.UpdateStatus();
                    Kinect.ProcessEvents();
                }
                catch (ThreadInterruptedException e)
                {
                    return;
                }
                catch (Exception ex)
                {

                }
            });
            updateThread.Start();


            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
            {
                Console.WriteLine("Tiefenkamera FPS: " + kinect.DepthCamera.Mode.FrameRate);
                Console.WriteLine("Farbkamera FPS: " + kinect.VideoCamera.Mode.FrameRate);

                LCD.SendMessage("X=" + Math.Round(kinect.Accelerometer.X, 2).ToString() + " Y=" + Math.Round(kinect.Accelerometer.Y, 2).ToString(), 1);
                LCD.SendMessage("Z=" + Math.Round(kinect.Accelerometer.Z, 2).ToString(), 2);
                Thread.Sleep(250);
                kinect.UpdateStatus();
            }

            Console.WriteLine("Programm beendet.");
            Console.ReadKey();
            LCD.Clear();
            LCD = null;
            kinect.Close();
            Kinect.Shutdown();
            kinect = null;
        }

        public static void ServoTest(Kinect kinect)
        {
            if (kinect.IsOpen) {
                Console.WriteLine("Kinect Tilt -1");
                kinect.Motor.Tilt = -1.0;
                Thread.Sleep(5000);

                Console.WriteLine("Kinect Tilt +1");
                kinect.Motor.Tilt = 1.0;
                Thread.Sleep(5000);

                Console.WriteLine("Kinect Tilt 0");
                kinect.Motor.Tilt = 0;
                Thread.Sleep(5000);
            }
        }

        public static void Init(Kinect kinect)
        { 
            if (Kinect.DeviceCount > 0)
            {
                // Connect to first device
                kinect.Open();

                // Setup event handlers
                kinect.VideoCamera.DataReceived += HandleKinectVideoCameraDataReceived;
                kinect.DepthCamera.DataReceived += HandleKinectDepthCameraDataReceived;

                // Start cameras
                kinect.VideoCamera.Start();
                kinect.DepthCamera.Start();


                // Set LED to Yellow
                kinect.LED.Color = LEDColor.Yellow;

                /*
                // Start update thread
                Thread t = new Thread(new ThreadStart(delegate ()
                {
                    while (true)
                    {
                        // Update status of accelerometer/motor etc.
                        kinect.UpdateStatus();

                        // Process any pending events.
                        Kinect.ProcessEvents();
                    }
                }));*/
            }
        }


        private static void HandleKinectVideoCameraDataReceived(object sender, BaseCamera.DataReceivedEventArgs e)
        {
            Console.WriteLine("Video data received at {0}", e.Timestamp);
            Mat img = new Mat(e.Data.Height, e.Data.Width, DepthType.Cv8U, 3);
            img.Save("RGB_img_mat_" + System.DateTime.Now.ToString().Replace(".", "").Replace("/", "").Replace(" ", "") + ".png");

            Image<Bgr, byte> depthImage = new Image<Bgr, byte>(e.Data.Width, e.Data.Height);
            depthImage.Bytes = e.Data.Data;
            depthImage.Save("rgb_img_" + System.DateTime.Now.ToString().Replace(".", "").Replace("/", "").Replace(" ", "") + ".png");
        }


        private static void HandleKinectDepthCameraDataReceived(object sender, BaseCamera.DataReceivedEventArgs e)
        {
            Console.WriteLine("Depth data received at {0}", e.Timestamp);
            Image<Gray, byte> depthImage = new Image<Gray, byte>(e.Data.Width, e.Data.Height);
            depthImage.Bytes = e.Data.Data;
            depthImage.Save("depth_img_" + System.DateTime.Now.ToString().Replace(".","").Replace("/", "").Replace(" ", "") + ".png");
        }



    }
}
