using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Drawing.Imaging;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Data.Services.Client;
using System.Drawing;

using Emgu.CV;
using Emgu.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Microsoft.Kinect;
using System.Windows.Threading;

namespace KinectCam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor myKinect;
        Skeleton[] totalSkeleton_0 = new Skeleton[6];
        Boolean trackingSkeleton = false;
        Boolean decisionNotYetMade = true;
        Boolean wearingGloves = false;
        Boolean wearingGown = false;
        Boolean wearingMask = false;

        int gownCount = 0;
        int all = 0;
        int glovesCount = 0;
        int maskCount = 0;

        int yellowCountUpper = 0;
        int yellowCountLower = 0;
        int blueCountUpper = 0;
        int blueCountLower = 0;
        int blueCountRight = 0;
        int blueCountLeft = 0;
        int whiteMaskColorCount = 0;
        int yellowMaskColorCount = 0;
        int greenMaskColorCount = 0;
        float oldDistance = 0;

        SkeletonPoint spine;
        SkeletonPoint hipLeft;
        SkeletonPoint kneeLeft;
        SkeletonPoint handLeft;
        SkeletonPoint handRight;
        SkeletonPoint head;
        SkeletonPoint shoulderCenter;
        SkeletonPoint NullPoint = new SkeletonPoint();

        byte[] upperData;
        byte[] lowerData;
        byte[] yellowUpperData;
        byte[] yellowLowerData;
        byte[] blueUpperData;
        byte[] blueLowerData;
        byte[] blueRightData;
        byte[] blueLeftData;
        byte[] maskData;

        int yellowCountUpperMax = 0;
        int yellowCountLowerMax = 0;
        int blueCountUpperMax = 0;
        int blueCountLowerMax = 0;
        int blueCountRightMax = 0;
        int blueCountLeftMax = 0;
        int maskColorCountMax = 0;
        int movement = -1;
        int direction = -1;
        String maskColor = "";

        private DispatcherTimer genTimer = new DispatcherTimer();
        private DispatcherTimer systemTimer = new DispatcherTimer();
        private int TimerTickCount = 0;
        private int SystemTickCount = 0;

        System.IO.StreamWriter file;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.genTimer.Interval = TimeSpan.FromSeconds(1);
            this.genTimer.Tick += genTimer_Tick;

            this.systemTimer.Interval = TimeSpan.FromSeconds(1);
            this.systemTimer.Tick += systemTimer_Tick;
            this.systemTimer.Start();

            myKinect = KinectSensor.KinectSensors[0];

            myKinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30 );
            
            myKinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(myKinect_ColorFrameReady);
            myKinect.ColorStream.CameraSettings.Contrast = 1.8;
            myKinect.ColorStream.CameraSettings.Gamma = 2.5;
            myKinect.ColorStream.CameraSettings.Saturation = 1.6;
            myKinect.ColorStream.CameraSettings.Brightness = 0.25;

            if (!myKinect.SkeletonStream.IsEnabled)
            {
                //Enable the skeleton steam with smooth parameters.
                //this.myKinect.DepthStream.Range = DepthRange.Near;
                //this.myKinect.SkeletonStream.EnableTrackingInNearRange = true;
                myKinect.SkeletonStream.Enable();
                //this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                myKinect.SkeletonFrameReady += myKinect_SkeletonFrameReady;
            }
            if (!myKinect.DepthStream.IsEnabled)
            {
                myKinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                myKinect.DepthFrameReady += myKinect_DepthFrameReady;
            }

            myKinect.Start();

            /* Writing data into a txt file */
            String path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Data\\data.txt";
            file = new System.IO.StreamWriter(path);

            file.WriteLine("=============================================================================");
            file.WriteLine("TITLE: MEDICAL EQUIPMENT DETECTION DATA");
            file.WriteLine("EXPERIMENT TIME: " + DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm\:ss tt"));
            file.WriteLine("=============================================================================");
            file.WriteLine("DETECTION HISTORY: ");
            file.WriteLine();
        }

        void genTimer_Tick(object sender, EventArgs e)
        {
            this.TimerTickCount++;
        }

        void systemTimer_Tick(object sender, EventArgs e)
        {
            this.SystemTickCount++;
        }

        private DepthImageFrame depthImageFrame = null;
        private short[] depthRawValues = new short[307200];
        void myKinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (depthImageFrame = e.OpenDepthImageFrame())
            {
                if (depthImageFrame != null)
                {
                    depthImageFrame.CopyPixelDataTo(depthRawValues);
                }
            }
        } 

        Skeleton firstSkeleton;
        void myKinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    Console.WriteLine("e.OpenSkeletonFrame() is null.");
                    return;
                }

                skeletonFrame.CopySkeletonDataTo(totalSkeleton_0);
                firstSkeleton = (from trackskeleton in totalSkeleton_0
                                          where trackskeleton.TrackingState == SkeletonTrackingState.Tracked
                                          select trackskeleton).FirstOrDefault();
                
                if (firstSkeleton == null)
                {
                    trackingSkeleton = false;

                    this.direction = Global.NONE;
                    this.movement = Global.NONE;
                    this.directionUI.Content = "";
                    this.movementUI.Content = "";

                    return;
                }
                else /* have skeleton and are not already tracking */
                {
                    if (firstSkeleton.Joints[JointType.Spine].TrackingState == JointTrackingState.Tracked)
                    {
                        spine = firstSkeleton.Joints[JointType.Spine].Position;
                        if (firstSkeleton.Joints[JointType.HipLeft].TrackingState == JointTrackingState.Tracked)
                            hipLeft = firstSkeleton.Joints[JointType.HipLeft].Position;
                        else
                            hipLeft = NullPoint;

                        if (firstSkeleton.Joints[JointType.KneeLeft].TrackingState == JointTrackingState.Tracked)
                            kneeLeft = firstSkeleton.Joints[JointType.KneeLeft].Position;
                        else
                            kneeLeft = NullPoint;

                        if (firstSkeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked)
                            handLeft = firstSkeleton.Joints[JointType.HandLeft].Position;
                        else
                            handLeft = NullPoint;

                        if (firstSkeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked)
                            handRight = firstSkeleton.Joints[JointType.HandRight].Position;
                        else
                            handRight = NullPoint;

                        if (firstSkeleton.Joints[JointType.ShoulderCenter].TrackingState == JointTrackingState.Tracked)
                            shoulderCenter = firstSkeleton.Joints[JointType.ShoulderCenter].Position;
                        else
                            shoulderCenter = NullPoint;

                        if (firstSkeleton.Joints[JointType.Head].TrackingState == JointTrackingState.Tracked)
                            head = firstSkeleton.Joints[JointType.Head].Position;
                        else
                            head = NullPoint;
                        
                        

                        if (oldDistance != 0)
                        {
                            if (oldDistance < spine.Z)
                            {
                                this.movementUI.Content = "LEAVING";
                                this.movement = Global.LEAVING;

                            }
                            else
                            {
                                this.movementUI.Content = "ENTERING";
                                this.movement = Global.ENTERING;
                            }
                        }

                        oldDistance = spine.Z;
                    }
                    //To decide if the person is facing to the camera or not
                    if (firstSkeleton.Joints[JointType.ElbowRight].Position.Z > firstSkeleton.Joints[JointType.HandRight].Position.Z)
                    {
                        this.directionUI.Content = "FACE TO CAMERA";
                        this.direction = Global.FACING_TO_CAMERA;
                    }
                    else
                    {
                        this.directionUI.Content = "BACK TO CAMERA";
                        this.direction = Global.BACK_FACING_TO_CAMERA;
                    }

                    if ( firstSkeleton != null && (trackingSkeleton == false) && (this.movement == Global.ENTERING )  && this.direction == Global.FACING_TO_CAMERA && spine.Z < Global.DISTANCE )
                    {

                        trackingSkeleton = true;
                        

                        yellowCountUpperMax = 0;
                        yellowCountLowerMax = 0;
                        blueCountUpperMax = 0;
                        blueCountLowerMax = 0;
                        blueCountRightMax = 0;
                        blueCountLeftMax = 0;
                        maskColorCountMax = 0;
                        maskColor = "";
                        TimerTickCount = 0;
                        this.genTimer.Start();
                        wearingGloves = false;
                        wearingGown = false;
                        wearingMask = false;
                        decisionNotYetMade = true;

                    }


                }
            }
        }

        void myKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                //Q?
                if ((trackingSkeleton == false) || (this.direction == Global.BACK_FACING_TO_CAMERA) ||
                (this.movement == Global.LEAVING) || ( TimerTickCount >= 1 ))
                    return;

                if (colorFrame == null) return;

                byte[] colorData = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(colorData);

                if (decisionNotYetMade)
                {
                    //Gloves detection
                    //RIGHT HAND
                    if (handRight != NullPoint)
                    {
                        ColorImagePoint handRight_cimg = this.myKinect.CoordinateMapper.MapSkeletonPointToColorPoint(handRight, ColorImageFormat.RgbResolution640x480Fps30);
                        blueCountRight = 0;
                        colorData = visualizeBorder(colorData, handRight_cimg);
                        int depth;
                        for (int i = handRight_cimg.Y - 7; i <= handRight_cimg.Y + 7; i++)
                        {
                            for (int j = handRight_cimg.X - 7; j <= handRight_cimg.X + 7; j++)
                            {
                                depth = depthRawValues[i * Global.CAMERA_WIDTH + j];
                                depth = depth >> 3;
                                // Only calculate those pixels whose depth is within the diameter of hand to avoid taking pixels from gown
                                if (Math.Abs(depth - handRight.Z * Global.METER_TO_MILIMETER) < Global.HAND_DIAMETER)
                                {
                                    int firstIndex = i * 640 * 4 + j * 4;
                                    // to avoid outOfBoundException
                                    if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3))
                                    {
                                        if (((int)colorData[firstIndex] < 255 && (int)colorData[firstIndex] > 130
                                        && (int)colorData[firstIndex + 1] < 120 && (int)colorData[firstIndex + 2] < 120)
                                        || ((int)colorData[firstIndex] > (int)colorData[firstIndex + 1] + 40
                                            && (int)colorData[firstIndex] > (int)colorData[firstIndex + 2] + 40))
                                        {
                                            blueCountRight++;
                                        }
                                    }
                                }
                            }
                        }
                         
                        /* Store the max blue color pixels on right hand and its associated image. */
                        if (blueCountRight > blueCountRightMax)
                        {
                            blueCountRightMax = blueCountRight;
                            blueRightData = colorData;
                        }
                    }

                    //LEFT HAND
                    if (handLeft != NullPoint)
                    {
                        ColorImagePoint handLeft_cimg = this.myKinect.CoordinateMapper.MapSkeletonPointToColorPoint(handLeft, ColorImageFormat.RgbResolution640x480Fps30);
                        blueCountLeft = 0;
                        colorData = visualizeBorder(colorData, handLeft_cimg);
                        int depth;
                        for (int i = handLeft_cimg.Y - 7; i <= handLeft_cimg.Y + 7; i++)
                        {
                            for (int j = handLeft_cimg.X - 7; j <= handLeft_cimg.X + 7; j++)
                            {
                                depth = depthRawValues[i * Global.CAMERA_WIDTH + j];
                                depth = depth >> 3;
                                // Only calculate those pixels whose depth is within the diameter of hand to avoid taking pixels from gown
                                if (Math.Abs(depth - handLeft.Z * Global.METER_TO_MILIMETER) < Global.HAND_DIAMETER)
                                {
                                    int firstIndex = i * 640 * 4 + j * 4;
                                    if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3)
                                        && (int)colorData[firstIndex] < 255 && (int)colorData[firstIndex] > 130
                                        && (int)colorData[firstIndex + 1] < 120 && (int)colorData[firstIndex + 2] < 120
                                         || ((int)colorData[firstIndex] > (int)colorData[firstIndex + 1] + 40
                                            && (int)colorData[firstIndex] > (int)colorData[firstIndex + 2] + 40))
                                    {
                                        blueCountLeft++;
                                    }
                                }
                            }
                        }
                        if (blueCountLeft > blueCountLeftMax )
                        {
                            blueCountLeftMax = blueCountLeft;
                            blueLeftData = colorData;
                        }
                    }

                    //Gown detection
                    //CHEST
                    if (spine != NullPoint)
                    {
                        ColorImagePoint spine_cimg = this.myKinect.CoordinateMapper.MapSkeletonPointToColorPoint(spine, ColorImageFormat.RgbResolution640x480Fps30);
                        yellowCountUpper = 0;
                        blueCountUpper = 0;
                        colorData = visualizeBorder(colorData, spine_cimg);
                        for (int i = spine_cimg.Y - 7; i <= spine_cimg.Y + 7; i++)
                        {
                            for (int j = spine_cimg.X - 7; j <= spine_cimg.X + 7; j++)
                            {
                                int firstIndex = i * 640 * 4 + j * 4;
                                // to avoid outOfBoundException

                                if ((firstIndex > 0 && firstIndex < ((int)colorData.Length - 3)
                                    && (int)colorData[firstIndex + 1] > (int)colorData[firstIndex] + Global.DIFFERENCE
                                        && (int)colorData[firstIndex + 2] > (int)colorData[firstIndex] + Global.DIFFERENCE))
                                {
                                    yellowCountUpper++;
                                }

                                if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3) && (int)colorData[firstIndex] > (int)colorData[firstIndex + 1] + 40
                                        && (int)colorData[firstIndex] > (int)colorData[firstIndex + 2] + 40)
                                {
                                    blueCountUpper++;
                                }
                            }
                        }

                        if (yellowCountUpper > yellowCountUpperMax)
                        {
                            yellowCountUpperMax = yellowCountUpper;
                            yellowUpperData = colorData;
                        }

                        if (blueCountUpper > blueCountUpperMax)
                        {
                            blueCountUpperMax = blueCountUpper;
                            blueUpperData = colorData;
                        }
                    }

                    //THGIH
                    if (hipLeft != NullPoint && kneeLeft != NullPoint)
                    {
                        ColorImagePoint thigh_cimg = this.myKinect.CoordinateMapper.MapSkeletonPointToColorPoint(maskPosition(kneeLeft, hipLeft), ColorImageFormat.RgbResolution640x480Fps30);

                        yellowCountLower = 0;
                        blueCountLower = 0;

                        colorData = visualizeBorder(colorData, thigh_cimg);
                        for (int i = thigh_cimg.Y - 7; i <= thigh_cimg.Y + 7; i++)
                        {
                            for (int j = thigh_cimg.X - 7; j <= thigh_cimg.X + 7; j++)
                            {
                                int firstIndex = i * 640 * 4 + j * 4;
                                // to avoid outOfBoundException

                                if ((firstIndex > 0 && firstIndex < ((int)colorData.Length - 3)
                                    && (int)colorData[firstIndex + 1] > (int)colorData[firstIndex] + Global.DIFFERENCE
                                        && (int)colorData[firstIndex + 2] > (int)colorData[firstIndex] + Global.DIFFERENCE))
                                {
                                    yellowCountLower++;
                                }
                                if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3) && (int)colorData[firstIndex] > (int)colorData[firstIndex + 1] + 40
                                       && (int)colorData[firstIndex] > (int)colorData[firstIndex + 2] + 40)
                                {
                                    blueCountLower++;
                                }
                            }
                        }

                        if (yellowCountLower > yellowCountLowerMax)
                        {
                            yellowCountLowerMax = yellowCountLower;
                            yellowLowerData = colorData;
                        }

                        if (blueCountLower > blueCountLowerMax)
                        {
                            blueCountLowerMax = blueCountLower;
                            blueLowerData = colorData;
                        }
                    }

                    // Mask Detection: Green

                    if (head != null && shoulderCenter != null)
                    {
                        ColorImagePoint mask_cimg = this.myKinect.CoordinateMapper.MapSkeletonPointToColorPoint(maskPosition(head, shoulderCenter), ColorImageFormat.RgbResolution640x480Fps30);
                        greenMaskColorCount = 0;
                        for (int i = mask_cimg.Y - 7; i <= mask_cimg.Y + 7; i++)
                        {
                            for (int j = mask_cimg.X - 7; j <= mask_cimg.X + 7; j++)
                            {
                                int firstIndex = i * 640 * 4 + j * 4;
                                // to avoid outOfBoundException
                                if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3)
                                    && (int)colorData[firstIndex] < 255 && (int)colorData[firstIndex] > 120
                                    && (int)colorData[firstIndex + 1] < 255 && (int)colorData[firstIndex + 1] > 120
                                    && (int)colorData[firstIndex + 2] < 120)
                                {
                                    greenMaskColorCount++;
                                }
                            }
                        }
                        if (greenMaskColorCount > maskColorCountMax)
                        {
                            maskColorCountMax = greenMaskColorCount;
                            maskData = colorData;
                            maskColor = "GREEN";
                        }


                        //Mask Detection: Yellow

                        yellowMaskColorCount = 0;
                        for (int i = mask_cimg.Y - 7; i <= mask_cimg.Y + 7; i++)
                        {
                            for (int j = mask_cimg.X - 7; j <= mask_cimg.X + 7; j++)
                            {
                                int firstIndex = i * 640 * 4 + j * 4;
                                // to avoid outOfBoundException
                                if ((firstIndex > 0 && firstIndex < ((int)colorData.Length - 3)
                                    && (int)colorData[firstIndex + 1] > (int)colorData[firstIndex] + 40
                                        && (int)colorData[firstIndex + 2] > (int)colorData[firstIndex] + 40
                                       )
                                  )
                                {
                                    yellowMaskColorCount++;
                                }
                            }
                        }
                        if (yellowMaskColorCount > maskColorCountMax)
                        {
                            maskColorCountMax = yellowMaskColorCount;
                            maskData = colorData;
                            maskColor = "YELLOW";
                        }


                        //Mask Detection: white
                        whiteMaskColorCount = 0;
                        colorData = visualizeBorder(colorData, mask_cimg);
                        for (int i = mask_cimg.Y - 7; i <= mask_cimg.Y + 7; i++)
                        {
                            for (int j = mask_cimg.X - 7; j <= mask_cimg.X + 7; j++)
                            {
                                int firstIndex = i * 640 * 4 + j * 4;
                                // to avoid outOfBoundException
                                if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3)
                                    && (int)colorData[firstIndex] < 255 && (int)colorData[firstIndex] > 200
                                    && (int)colorData[firstIndex + 1] < 255 && (int)colorData[firstIndex + 1] > 200
                                    && (int)colorData[firstIndex + 2] < 230 && (int)colorData[firstIndex + 2] > 160)
                                {
                                    whiteMaskColorCount++;
                                }
                            }
                        }

                        if (whiteMaskColorCount > maskColorCountMax)
                        {
                            maskColorCountMax = whiteMaskColorCount;
                            maskData = colorData;
                            maskColor = "WHITE";
                        }
                    }
                }

                String detectString = "";
                if (decisionNotYetMade == true && TimerTickCount >= 1)
                {

                    decisionNotYetMade = false;

                    //Decide Gloves
                    if ( blueCountRightMax > 50 && blueCountLeftMax > 50)
                    {
                        wearingGloves = true;
                        detectString += "(GLOVES)";
                    }

                    this.gownColorUI.Content = "Yellow";
                    this.gownColorUI2.Content = "Yellow";
                    this.pixelsOnChest.Content = yellowCountUpperMax;
                    this.pixelsOnThigh.Content = yellowCountLowerMax;
                    upperData = yellowUpperData;
                    lowerData = yellowLowerData;
                    //Decide Gown
                    if ((yellowCountLowerMax > 60 && yellowCountUpperMax > 10) || yellowCountLowerMax > 10 && yellowCountUpperMax > 60 || (yellowCountUpperMax > 20 && yellowCountLowerMax > 20))
                    {
                        wearingGown = true;
                        detectString += "(YELLOW GOWN)";
                        
                    }

                    if (blueCountLowerMax > 100 && blueCountUpperMax > 100)
                    {
                        wearingGown = true;
                        this.gownColorUI.Content = "Blue";
                        this.gownColorUI2.Content = "Blue";
                        detectString += "(BLUE GOWN)";
                        upperData = blueUpperData;
                        lowerData = blueLowerData;
                        this.pixelsOnChest.Content = blueCountUpperMax;
                        this.pixelsOnThigh.Content = blueCountLowerMax;
                    }

                    //Decide Mask
                    if (maskColorCountMax > 60)
                    {
                        wearingMask = true;
                        detectString += ("(" +  maskColor + " MASK)");
                    }

                    if (wearingMask)
                    {
                        maskCount += 1;
                        this.maskDetect.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        this.maskDetect.Visibility = System.Windows.Visibility.Hidden;
                    }

                    if (wearingGown)
                    {
                        gownCount += 1;
                        this.gownDetect.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        this.gownDetect.Visibility = System.Windows.Visibility.Hidden;
                    }

                    if (wearingGloves)
                    {
                        glovesCount += 1;
                        this.glovesDetect.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        this.glovesDetect.Visibility = System.Windows.Visibility.Hidden;
                    }

                    if (!(wearingGloves || wearingGown || wearingMask))
                    {
                        this.noneDetect.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        this.noneDetect.Visibility = System.Windows.Visibility.Hidden;
                    }

                    this.bluePixelsLeft.Content = blueCountLeftMax;
                    this.bluePixelsRight.Content = blueCountRightMax;
                    
                    this.pixelsFace.Content = maskColorCountMax;
                    this.maskColorUI.Content = maskColor;
             

                    all += 1;
                    this.total.Content = all;
                    this.gloves.Content = glovesCount;
                    this.gown.Content = gownCount;
                    this.mask.Content = maskCount;
                    
                    file.WriteLine("PERSON " + this.all + ":");
                    file.WriteLine("MOVEMENT: " + this.movementUI.Content);
                    file.WriteLine("DIRECTION: " + this.directionUI.Content);
                    file.WriteLine("TIME(SINCE START): " + this.SystemTickCount/60 + "(M) " + this.SystemTickCount % 60 + "(S)");
                    if (detectString.Equals(""))
                        detectString = "NONE";
                    file.WriteLine("DETECT: " + detectString);
                    file.WriteLine();

                    /*Clean up*/
                    this.genTimer.Stop();
                }


                kinectVideo.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height,
                        96, 96, PixelFormats.Bgr32, null, colorData, colorFrame.Width * colorFrame.BytesPerPixel);
            }
        }

        private Boolean isInLowerBody(CircleF circle)
        {
            DepthImagePoint point = new DepthImagePoint();
            point.X = (int)circle.Center.X;
            point.Y = (int)circle.Center.Y;
            point.Depth = depthRawValues[point.Y * 640 + point.X]; // 640 means the width of the frame.
            point.Depth = point.Depth >> 3;

            SkeletonPoint spoint = myKinect.CoordinateMapper.MapDepthPointToSkeletonPoint(DepthImageFormat.Resolution640x480Fps30,point);

            if (firstSkeleton != null && Math.Abs(spoint.X - hipLeft.X) < 0.2 && spoint.Y < hipLeft.Y - 0.1 && spoint.Y > kneeLeft.Y && Math.Abs(spoint.Z - hipLeft.Z) < 0.2)
            {
                return true;
            }
            return false;
        }

        private Boolean isInUpperBody(CircleF circle)
        {
            DepthImagePoint point = new DepthImagePoint();
            point.X = (int)circle.Center.X;
            point.Y = (int)circle.Center.Y;
            point.Depth = depthRawValues[point.Y * 640 + point.X]; // 640 means the width of the frame.
            point.Depth = point.Depth >> 3;

            SkeletonPoint spoint = myKinect.CoordinateMapper.MapDepthPointToSkeletonPoint(DepthImageFormat.Resolution640x480Fps30, point);

            if (firstSkeleton != null && Math.Abs(spoint.X -   spine.X) < 0.2 &&  Math.Abs(spoint.Y - spine.Y) < 0.1)
            {
                return true;
            }
            return false;
        }

        private Boolean isInHands(CircleF circle)
        {
            DepthImagePoint point = new DepthImagePoint();
            point.X = (int)circle.Center.X;
            point.Y = (int)circle.Center.Y;
            point.Depth = depthRawValues[point.Y * 640 + point.X]; // 640 means the width of the frame.
            point.Depth = point.Depth >> 3;
            Console.WriteLine(point.Depth);
            SkeletonPoint spoint = myKinect.CoordinateMapper.MapDepthPointToSkeletonPoint(DepthImageFormat.Resolution640x480Fps30, point);

            if (firstSkeleton != null && Math.Abs(spoint.X - handLeft.X) < 0.1 && Math.Abs(spoint.Y - handLeft.Y) < 0.1 && Math.Abs(spoint.Z - handLeft.Z) < 0.1)
            {
                return true;
            }
            else if (firstSkeleton != null && Math.Abs(spoint.X - handRight.X) < 0.1 && Math.Abs(spoint.Y - handRight.Y) < 0.1 && Math.Abs(spoint.Z - handRight.Z) < 0.1)
            {
                return true;
            }
            return false;
        }

        private SkeletonPoint maskPosition(SkeletonPoint head, SkeletonPoint shoulderCenter)
        {
            SkeletonPoint mask = new SkeletonPoint();
            mask.X = ( head.X + shoulderCenter.X ) / 2;
            mask.Z = ( head.Z + shoulderCenter.Z ) / 2;
            mask.Y = (head.Y + shoulderCenter.Y) / 2;
            mask.Y = (mask.Y + head.Y) / 2;

            return mask;
        }

        private SkeletonPoint thighPosition(SkeletonPoint kneeLeft, SkeletonPoint hipLeft)
        {
            SkeletonPoint thigh = new SkeletonPoint();
            thigh.X = (kneeLeft.X + hipLeft.X) / 2;
            thigh.Z = (kneeLeft.Z + hipLeft.Z) / 2;
            thigh.Y = (kneeLeft.Y + hipLeft.Y) / 2;

            return thigh;
        }

        /* THe purpose of the function is to visualize the area of the detecting region.*/
        private byte[] visualizeBorder(byte[] colorData, ColorImagePoint center) {


            for (int x = center.X - 8; x <= center.X + 8; x++)
            {
                int firstIndex = 640 * 4 * (center.Y - 8) + x * 4;
                if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3))
                {
                    colorData[firstIndex] = 255;
                    colorData[firstIndex+1] = 255;
                    colorData[firstIndex+2] = 0;
                }
            }

            for (int x = center.X - 8; x <= center.X + 8; x++)
            {
                int firstIndex = 640 * 4 * (center.Y + 8) + x * 4;
                if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3))
                {
                    colorData[firstIndex] = 255;
                    colorData[firstIndex + 1] = 255;
                    colorData[firstIndex + 2] = 0;
                }
            }

            for (int y = center.Y - 8; y <= center.Y + 8; y++)
            {
                int firstIndex = 640 * 4 * y +  (center.X - 8)* 4;
                if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3))
                {
                    colorData[firstIndex] = 255;
                    colorData[firstIndex + 1] = 255;
                    colorData[firstIndex + 2] = 0;
                }
            }

            for (int y = center.Y - 8; y <= center.Y + 8; y++)
            {
                int firstIndex = 640 * 4 * y + (center.X + 8) * 4;
                if (firstIndex > 0 && firstIndex < ((int)colorData.Length - 3))
                {
                    colorData[firstIndex] = 255;
                    colorData[firstIndex + 1] = 255;
                    colorData[firstIndex + 2] = 0;
                }
            }

            return colorData;
        }

        private void gamma_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.myKinect != null && this.myKinect.ColorStream.IsEnabled)
            {
                this.myKinect.ColorStream.CameraSettings.Gamma = e.NewValue;
            }
        }

        private void contrast_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.myKinect != null && this.myKinect.ColorStream.IsEnabled)
            {
                this.myKinect.ColorStream.CameraSettings.Contrast = e.NewValue;
            }
        }

        private void saturation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.myKinect != null && this.myKinect.ColorStream.IsEnabled)
            {
                this.myKinect.ColorStream.CameraSettings.Saturation = e.NewValue;
            }
        }

        private void brightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.myKinect != null && this.myKinect.ColorStream.IsEnabled)
            {
                this.myKinect.ColorStream.CameraSettings.Brightness = e.NewValue;
            }
        }

        private void Button_Click_leftHand(object sender, RoutedEventArgs e)
        {
            if (blueLeftData != null)
            {
                kinectVideo2.Source = BitmapSource.Create(640, 480,
                            96, 96, PixelFormats.Bgr32, null, blueLeftData, 640 * 4);
            }
        }

        private void Button_Click_rightHand(object sender, RoutedEventArgs e)
        {
            if (blueRightData != null)
            {
                kinectVideo2.Source = BitmapSource.Create(640, 480,
                            96, 96, PixelFormats.Bgr32, null, blueRightData, 640 * 4);
            }
        }
        private void Button_Click_chest(object sender, RoutedEventArgs e)
        {
            if (upperData != null)
            {
                kinectVideo2.Source = BitmapSource.Create(640, 480,
                            96, 96, PixelFormats.Bgr32, null, upperData, 640 * 4);
            }
        }
        private void Button_Click_thigh(object sender, RoutedEventArgs e)
        {
            if (lowerData != null)
            {
                kinectVideo2.Source = BitmapSource.Create(640, 480,
                            96, 96, PixelFormats.Bgr32, null, lowerData, 640 * 4);
            }
        }
        private void Button_Click_face(object sender, RoutedEventArgs e)
        {
            if (maskData != null)
            {
                kinectVideo2.Source = BitmapSource.Create(640, 480,
                            96, 96, PixelFormats.Bgr32, null, maskData, 640 * 4);
            }
        }



        private void Window_Closed(object sender, EventArgs e)
        {
            this.systemTimer.Stop();
            file.WriteLine("=====================================SUMMARY=================================");
            file.WriteLine("PEOPLE DETECTED: " + this.all);
            file.WriteLine("GLOVES DETECTED: " + this.glovesCount);
            file.WriteLine("GOWN   DETECTED: " + this.gownCount);
            file.WriteLine("MASK   DETECTED: " + this.maskCount);
            file.Flush();
            file.Close();
        }
       
    }
}
