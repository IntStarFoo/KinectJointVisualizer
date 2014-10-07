using FiveTwoFiveTwo.KinectMathHelpers;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace K4WJointVisualizer.ViewModels
{
    public class KinectBaseViewModel : ViewModelBase
    {

        #region class variables

        /// <summary>
        /// Size of the RGB pixel in the _kinectColorBitmap
        /// </summary>
        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for depth/color/body index frames
        /// </summary>
        private MultiSourceFrameReader multiFrameSourceReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap _kinectColorBitmap = null;

        /// <summary>
        /// The size in bytes of the _kinectColorBitmap back buffer
        /// </summary>
        private uint bitmapBackBufferSize = 0;

        /// <summary>
        /// Intermediate storage for the color to depth mapping
        /// </summary>
        private DepthSpacePoint[] colorMappedToDepthPoints = null;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;


        /// <summary>
        /// Gets the _kinectColorBitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this._kinectColorBitmap;
            }
        }


        private int _displayWidth;
        private int _displayHeight;
        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource2
        {
            get
            {
                return this._imageSource;
            }
        }
        private ImageSource _imageSource;
        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;


        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.SetProperty(ref statusText, value, () => this.StatusText);
                }
            }
        }


        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;


        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;


        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 1;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 1, 1, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);




        private bool _drawOrientationVectors;
        public bool DrawOrientationVectors
        {
            get
            {
                return _drawOrientationVectors;
            }
            set
            {

                this.SetProperty(ref _drawOrientationVectors, value, () => this.DrawOrientationVectors);
            }
        }

        private bool _drawOrientationAnglesX;
        public bool DrawOrientationAnglesX
        {
            get
            {
                return _drawOrientationAnglesX;
            }
            set
            {

                this.SetProperty(ref _drawOrientationAnglesX, value, () => this.DrawOrientationAnglesX);
            }
        }
        private bool _drawOrientationAnglesY;
        public bool DrawOrientationAnglesY
        {
            get
            {
                return _drawOrientationAnglesY;
            }
            set
            {

                this.SetProperty(ref _drawOrientationAnglesY, value, () => this.DrawOrientationAnglesY);
            }
        }
        private bool _drawOrientationAnglesZ;
        public bool DrawOrientationAnglesZ
        {
            get
            {
                return _drawOrientationAnglesZ;
            }
            set
            {

                this.SetProperty(ref _drawOrientationAnglesZ, value, () => this.DrawOrientationAnglesZ);
            }
        }


        #endregion


        #region ViewModel Commands




        private CommandBase _screenshotCommand { get; set; }
        public CommandBase ScreenshotCommand
        {
            get
            {
                if (_screenshotCommand == null)
                {
                    _screenshotCommand = new CommandBase(i => ScreenshotCommandExecute(), null);
                }
                return _screenshotCommand;
            }
        }

        #endregion

        #region constructor singleton instance


        private KinectBaseViewModel()
        {

        }



        private static KinectBaseViewModel _instance;
        /// <summary>
        /// Singleton instance of this VM.
        /// </summary>
        public static KinectBaseViewModel Instance
        {

            get
            {
                if (_instance == null)
                {
                    _instance = new KinectBaseViewModel();

                    // for Alpha (and public beta!!! boo.) one sensor is supported
                    _instance.kinectSensor = KinectSensor.GetDefault();

                    if (_instance.kinectSensor != null)
                    {
                        _instance.multiFrameSourceReader = _instance.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);

                        _instance.multiFrameSourceReader.MultiSourceFrameArrived += _instance.Reader_MultiSourceFrameArrived;

                        _instance.coordinateMapper = _instance.kinectSensor.CoordinateMapper;

                        FrameDescription depthFrameDescription = _instance.kinectSensor.DepthFrameSource.FrameDescription;

                        int depthWidth = depthFrameDescription.Width;
                        int depthHeight = depthFrameDescription.Height;

                        FrameDescription colorFrameDescription = _instance.kinectSensor.ColorFrameSource.FrameDescription;

                        int colorWidth = colorFrameDescription.Width;
                        int colorHeight = colorFrameDescription.Height;

                        _instance.colorMappedToDepthPoints = new DepthSpacePoint[colorWidth * colorHeight];


                        _instance._displayWidth = depthWidth;
                        _instance._displayHeight = depthHeight;
                        _instance.InitializeBodyVisuals();

                        _instance.DrawOrientationVectors = false;


                        _instance.DrawOrientationAnglesX = false;

                        _instance.DrawOrientationAnglesY = false;
                        _instance.DrawOrientationAnglesZ = false;

                        // Create the drawing group we'll use for drawing
                        _instance.drawingGroup = new DrawingGroup();
                        // Create an image source that we can use in our image control
                        _instance._imageSource = new DrawingImage(_instance.drawingGroup);

                        _instance._kinectColorBitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);

                        // Calculate the WriteableBitmap back buffer size
                        _instance.bitmapBackBufferSize = (uint)((_instance._kinectColorBitmap.BackBufferStride * (_instance._kinectColorBitmap.PixelHeight - 1)) + (_instance._kinectColorBitmap.PixelWidth * _instance.bytesPerPixel));

                        _instance.kinectSensor.IsAvailableChanged += _instance.Sensor_IsAvailableChanged;

                        _instance.kinectSensor.Open();

                        _instance.StatusText = _instance.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                                        : Properties.Resources.NoSensorStatusText;


                    }
                    else
                    {
                    }



                }

                return _instance;
            }
        }


        private void InitializeBodyVisuals()
        {
            // populate body colors, one for each BodyIndex
            Instance.bodyColors = new List<Pen>();

            Instance.bodyColors.Add(new Pen(Brushes.Violet, 2));
            Instance.bodyColors.Add(new Pen(Brushes.Orange, 2));
            Instance.bodyColors.Add(new Pen(Brushes.Green, 2));
            Instance.bodyColors.Add(new Pen(Brushes.Blue, 2));
            Instance.bodyColors.Add(new Pen(Brushes.Indigo, 2));
            Instance.bodyColors.Add(new Pen(Brushes.Red, 2));
            // a bone defined as a line between two joints
            Instance.bones = new List<Tuple<JointType, JointType>>();
            // Torso
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            Instance.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

        }



        #endregion

        #region viewmodel event handlers
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            Instance.StatusText = Instance.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
        }

        /// <summary>
        /// Handles the depth/color/body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            DepthFrame depthFrame = null;
            ColorFrame colorFrame = null;
            BodyIndexFrame bodyIndexFrame = null;
            BodyFrame bodyFrame = null;

            bool isBitmapLocked = false;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }

            // We use a try/finally to ensure that we clean up before we exit the function.  
            // This includes calling Dispose on any Frame objects that we may have and unlocking the _kinectColorBitmap back buffer.
            try
            {
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
                bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();

                bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();
                if (bodyFrame != null)
                {
                    this.RenderBodyFrame(bodyFrame);
                }

                // If any frame has expired by the time we process this event, return.
                // The "finally" statement will Dispose any that are not null.
                if ((depthFrame == null) || (colorFrame == null) || (bodyIndexFrame == null))
                {
                    return;
                }
            }
            finally
            {
                if (isBitmapLocked)
                {
                    this._kinectColorBitmap.Unlock();
                }

                if (depthFrame != null)
                {
                    depthFrame.Dispose();
                }

                if (bodyFrame != null)
                {
                    bodyFrame.Dispose();
                }

                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                }

                if (bodyIndexFrame != null)
                {
                    bodyIndexFrame.Dispose();
                }
            }
        }

        /// <summary>
        /// Handles the user invoking the screenshot command
        ///   Creates a .png file in Environment.SpecialFolder.MyPictures
        ///   with the contents of the ImageSource
        /// </summary>
        private void ScreenshotCommandExecute()
        {
            // Create a render target to which we'll render our composite image
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)_kinectColorBitmap.Width, (int)_kinectColorBitmap.Height, 96.0, 96.0, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawImage(ImageSource, new Rect(new Point(), new Size(_kinectColorBitmap.Width, _kinectColorBitmap.Height)));
            }

            renderBitmap.Render(dv);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = Path.Combine(myPhotos, "KinectScreenshot-" + time + ".png");

            // Write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
            }
            catch (IOException)
            {
                this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
            }
        }

        private void RenderBodyFrame(BodyFrame bodyFrame)
        {
            bool dataReceived = false;

            Body[] bodies = null;
            if (bodyFrame != null)
            {
                if (bodies == null)
                {
                    bodies = new Body[bodyFrame.BodyCount];
                }

                // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                // As long as those body objects are not disposed and not set to null in the array,
                // those body objects will be re-used.
                bodyFrame.GetAndRefreshBodyData(bodies);
                dataReceived = true;
            }

            if (dataReceived)
            {

                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, Instance._displayWidth, Instance._displayHeight));

                    int penIndex = 0;
                    foreach (Body body in bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen, body.JointOrientations);
                            if (Instance.DrawOrientationVectors == true)
                            {
                                this.DrawLocalCoordinates(body.JointOrientations, joints, jointPoints, dc);
                            }
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, Instance._displayWidth, Instance._displayHeight));
                }


            }
        }




        private void DrawLocalCoordinates(IReadOnlyDictionary<JointType, JointOrientation> jointOrientations
                                            , IReadOnlyDictionary<JointType, Joint> joints
                                            , IDictionary<JointType, Point> jointPoints
                                            , DrawingContext drawingContext)
        {
            Pen xCoordPen = new Pen(Brushes.Red, 2);
            Pen yCoordPen = new Pen(Brushes.Green, 2);
            Pen zCoordPen = new Pen(Brushes.Blue, 2);
            //Draw the local coordinates
            foreach (JointType jointType in jointOrientations.Keys)
            {
                Vector4 vec = jointOrientations[jointType].Orientation;
                Quaternion qOrientation = new Quaternion(vec.W, vec.X, vec.Y, vec.Z);
                CameraSpacePoint csX = CreateEndPoint(joints[jointType].Position, qOrientation.Rotate(0.1f, 0.0f, 0.0f));
                CameraSpacePoint csY = CreateEndPoint(joints[jointType].Position, qOrientation.Rotate(0.0f, 0.1f, 0.0f));
                CameraSpacePoint csZ = CreateEndPoint(joints[jointType].Position, qOrientation.Rotate(0.0f, 0.0f, 0.1f));

                DepthSpacePoint dsX = this.coordinateMapper.MapCameraPointToDepthSpace(csX);
                DepthSpacePoint dsY = this.coordinateMapper.MapCameraPointToDepthSpace(csY);
                DepthSpacePoint dsZ = this.coordinateMapper.MapCameraPointToDepthSpace(csZ);

                drawingContext.DrawLine(xCoordPen, jointPoints[jointType], new Point(dsX.X, dsX.Y));
                drawingContext.DrawLine(yCoordPen, jointPoints[jointType], new Point(dsY.X, dsY.Y));
                drawingContext.DrawLine(zCoordPen, jointPoints[jointType], new Point(dsZ.X, dsZ.Y));

                if (Instance.DrawOrientationAnglesX == true
                    || Instance.DrawOrientationAnglesY == true
                    || Instance.DrawOrientationAnglesZ == true)
                {

                    JointType parentJoint = KinectHelpers.GetParentJoint(jointType);
                    double AngleBetweenParentChildY = 0;
                    double AngleBetweenParentChildX = 0;
                    double AngleBetweenParentChildZ = 0;

                    //For each vector in the DepthSpacePoint, compute the angle between 
                    //  parent and child (only if the joint has a parent)
                    if (parentJoint != jointType)
                    {

                        Vector4 vecParent = jointOrientations[parentJoint].Orientation;
                        Quaternion qOrientationParent = new Quaternion(vecParent.W, vecParent.X, vecParent.Y, vecParent.Z);
                        //(only compute if requested) 
                        if (DrawOrientationAnglesX == true)
                        {
                            CameraSpacePoint csXParent = CreateEndPoint(joints[parentJoint].Position, qOrientationParent.Rotate(0.1f, 0.0f, 0.0f));
                            DepthSpacePoint dsXParent = this.coordinateMapper.MapCameraPointToDepthSpace(csXParent);
                            AngleBetweenParentChildX = MathHelpers.AngleBetweenPoints(new Point(dsX.X, dsX.Y), new Point(dsXParent.X, dsXParent.Y));
                        }
                        if (DrawOrientationAnglesY == true)
                        {
                            CameraSpacePoint csYParent = CreateEndPoint(joints[parentJoint].Position, qOrientationParent.Rotate(0.0f, 0.1f, 0.0f));
                            DepthSpacePoint dsYParent = this.coordinateMapper.MapCameraPointToDepthSpace(csYParent);
                            AngleBetweenParentChildY = MathHelpers.AngleBetweenPoints(new Point(dsY.X, dsY.Y), new Point(dsYParent.X, dsYParent.Y));
                        }
                        if (DrawOrientationAnglesZ == true)
                        {
                            CameraSpacePoint csZParent = CreateEndPoint(joints[parentJoint].Position, qOrientationParent.Rotate(0.0f, 0.0f, 0.1f));
                            DepthSpacePoint dsZParent = this.coordinateMapper.MapCameraPointToDepthSpace(csZParent);
                            AngleBetweenParentChildZ = MathHelpers.AngleBetweenPoints(new Point(dsZ.X, dsY.Y), new Point(dsZParent.X, dsZParent.Y));
                        }
                    }

                    if (DrawOrientationAnglesY == true)
                    {
                        //Fun With Formatted Text
                        ///http://msdn.microsoft.com/en-us/library/system.windows.media.formattedtext(v=vs.110).aspx
                        FormattedText fteY = new FormattedText(String.Format("{0,0:F0}°", AngleBetweenParentChildY),
                                                                    CultureInfo.GetCultureInfo("en-us"),
                                                                    FlowDirection.LeftToRight,
                                                                    new Typeface("Verdana"),
                                                                    8, yCoordPen.Brush);

                        drawingContext.DrawText(fteY, GetTextTopLeft(fteY, new Point(dsY.X, dsY.Y), jointPoints[jointType]));

                    }
                    if (DrawOrientationAnglesZ == true)
                    {
                        FormattedText fteZ = new FormattedText(String.Format("{0,0:F0}°", AngleBetweenParentChildZ),
                                                                    CultureInfo.GetCultureInfo("en-us"),
                                                                    FlowDirection.LeftToRight,
                                                                    new Typeface("Verdana"),
                                                                    8, zCoordPen.Brush);

                        drawingContext.DrawText(fteZ, GetTextTopLeft(fteZ, new Point(dsZ.X, dsZ.Y), jointPoints[jointType]));
                    }
                    if (DrawOrientationAnglesX == true)
                    {
                        FormattedText fteX = new FormattedText(String.Format("{0,0:F0}°", AngleBetweenParentChildX),
                                                                    CultureInfo.GetCultureInfo("en-us"),
                                                                    FlowDirection.LeftToRight,
                                                                    new Typeface("Verdana"),
                                                                    8, xCoordPen.Brush);

                        drawingContext.DrawText(fteX, GetTextTopLeft(fteX, new Point(dsX.X, dsX.Y), jointPoints[jointType]));
                    }
                }
            }
        }
        /// <summary>
        /// Computes a good position for drawing text next to a line 
        ///   If the line goes from X+ -> X-, The END of the text ends at X-
        ///   If the line goes from X- -> X+, the BEGINNING of the text starts at X+
        ///   //  like this<------  ------>or this 
        /// </summary>
        /// <param name="ftxt">Formatted Text to be displayed</param>
        /// <param name="lineTo">End of line</param>
        /// <param name="lineFrom">Start of line</param>
        /// <returns></returns>
        private Point GetTextTopLeft(FormattedText ftxt, Point lineTo, Point lineFrom)
        {
            Point retval = lineTo;
            // Try to place the text outside of the body but connected to the line...
            if (lineFrom.X > lineTo.X + 10)
            {
                retval.X = lineTo.X - ftxt.Width;
            }
            return retval;
        }

        private CameraSpacePoint CreateEndPoint(CameraSpacePoint startP, float[] vec)
        {
            CameraSpacePoint point = new CameraSpacePoint();
            point.X = startP.X + vec[0];
            point.Y = startP.Y + vec[1];
            point.Z = startP.Z + vec[2];
            return point;
        }



        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen, IReadOnlyDictionary<JointType, JointOrientation> JointOrientations)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);

                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }


        #endregion



    }
}