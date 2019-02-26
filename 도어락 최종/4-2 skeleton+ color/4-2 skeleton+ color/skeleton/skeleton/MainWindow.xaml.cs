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
using Microsoft.Kinect;
using System.Windows.Threading;
using System.IO.Ports;

namespace skeleton
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort arduSerialPort = new SerialPort();
        KinectSensor _sensor;
        DispatcherTimer Timer = new DispatcherTimer();
        protect mess = new protect();
        waveGesture gesture = new waveGesture();
        private Skeleton[] _FrameSkeletons;
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private double rightangle = 0;
        private double leftangle = 0;
        private double shangle = 0;
        private int code = 0;
        private int code2 = 0;
        

        public MainWindow()
        {
            InitializeComponent();
            arduSerialPort.PortName = "COM5";
            arduSerialPort.BaudRate = 9600;
            arduSerialPort.Open();
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChange;
            this.KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        //--------------센서 초기화
        private void KinectSensors_StatusChange(object sender, StatusChangedEventArgs e)
        {
            switch(e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                    this.KinectDevice = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    this.KinectDevice = null;
                    break;
                default:
                    break;
            }
        }

        public KinectSensor KinectDevice
        {
            get
            {
                return this._sensor;
            }
            set
            {
                if(this._sensor != value)
                {
                    if(this._sensor != null)
                    {
                        this._sensor.Stop();
                        this._sensor.SkeletonFrameReady -= Kinect_SkeletonFreamReady;
                        this._sensor.ColorFrameReady -= Kinect_ColorFreamReady;
                        this._sensor.ColorStream.Disable();
                        this._sensor.DepthStream.Disable();
                        this._sensor.SkeletonStream.Disable();
                        this._FrameSkeletons = null;
                    }
                    this._sensor = value;

                    if(this._sensor != null)
                    {
                        if(this._sensor.Status == KinectStatus.Connected)
                        {
                            this._sensor.SkeletonStream.Enable();
                            this._sensor.DepthStream.Enable();
                            this._sensor.ColorStream.Enable();
                            this._FrameSkeletons = new Skeleton[this._sensor.SkeletonStream.FrameSkeletonArrayLength];
                            this._sensor.SkeletonFrameReady += Kinect_SkeletonFreamReady;
                            this._sensor.ColorFrameReady += Kinect_ColorFreamReady;
                            ColorImageBitmap(_sensor);
                            this._sensor.Start();
                        }
                    }
                }
            }
        }

        //-----------------컬러 비트맵 초기화
        private void ColorImageBitmap(KinectSensor sensor)            
        {


            ColorImageStream colorStream = sensor.ColorStream;
            _ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight, 96, 96,
                                                         PixelFormats.Bgr32, null);                        
            _ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight); 
            _ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;                 
            
            colorimage.Source = _ColorImageBitmap;
        }

        private void Kinect_ColorFreamReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);

                    _ColorImageBitmap.WritePixels(_ColorImageBitmapRect, pixelData, _ColorImageStride, 0);

                }
            }
        }

        //------------------------키넥트 메인 동작
        private void Kinect_SkeletonFreamReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    frame.CopySkeletonDataTo(this._FrameSkeletons);
                    Skeleton skeleton = GetPrimarySkeleton(this._FrameSkeletons);
                    if (skeleton != null)
                    {

                        tr.Text = "tracking"; //트래킹 확인
                        //------------필요한 관절 위치 따오기
                        Point rightelbow = GetJointPoint(skeleton.Joints[JointType.ElbowRight]);
                        Point rightshoulder = GetJointPoint(skeleton.Joints[JointType.ShoulderRight]);
                        Point leftelbow = GetJointPoint(skeleton.Joints[JointType.ElbowLeft]);
                        Point leftshoudler = GetJointPoint(skeleton.Joints[JointType.ShoulderLeft]);

                        // 타이머 + 포즈 메서드
                        shangle = GetJointAngle(skeleton.Joints[JointType.HipRight], skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight]);
                        rightangle = GetJointAngle(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.HandRight]);
                        leftangle = GetJointAngle(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.HandLeft]);

                        code = mess.Update(rightangle, leftangle, rightelbow, rightshoulder, leftelbow, leftshoudler);
                        code2 = gesture.Update(this._FrameSkeletons, shangle);
                    }
                }
            }
            if (code == 1 && code2 == 0)
            {
                arduSerialPort.Write("0");
                TextBox.Text="닫힘";
            }
            else if(code2 == 1)
            {
                arduSerialPort.Write("1");
                TextBox.Text = "열림";
                code = 0;
            }
        }




        //------------------사용자 우선순위

        private Skeleton GetPrimarySkeleton(Skeleton[] Skeletons)
        {
            Skeleton skeleton = null;

            if (Skeletons != null)
            {

                for (int i = 0; i < Skeletons.Length; i++)
                {
                    if (Skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = Skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > Skeletons[i].Position.Z)
                            {
                                skeleton = Skeletons[i];
                            }
                        }
                    }
                }
            }
            return skeleton;
        }
        //--------------------각도 계산

        private double GetJointAngle(Joint leftjoint, Joint centerjoint, Joint rightjoint)
        {
            Point leftPoint = GetJointPoint(leftjoint);
            Point centerPoint = GetJointPoint(centerjoint);
            Point rightPoint = GetJointPoint(rightjoint);

            double a = Math.Sqrt(Math.Pow((leftPoint.X - centerPoint.X), 2) + Math.Pow((leftPoint.Y - centerPoint.Y), 2));
            double b = Math.Sqrt(Math.Pow((centerPoint.X - rightPoint.X), 2) + Math.Pow((centerPoint.Y - rightPoint.Y), 2));
            double c = Math.Sqrt(Math.Pow((leftPoint.X - rightPoint.X), 2) + Math.Pow((leftPoint.Y - rightPoint.Y), 2));

            double anglerad = Math.Acos((a * a + b * b - c * c) / (2 * a * b));
            double angleDeg = anglerad*180 / Math.PI;

            return angleDeg;
        }
    
        //----------------------------스켈레톤 조인트 포인트


        private Point GetJointPoint(Joint joint)
        {
            DepthImagePoint point = this._sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, this._sensor.DepthStream.Format);

            point.X *= (int)this.Layout.ActualWidth / this._sensor.DepthStream.FrameWidth;
            point.Y *= (int)this.Layout.ActualHeight / this._sensor.DepthStream.FrameHeight;

            return new Point(point.X, point.Y);
        }

        
    }
}