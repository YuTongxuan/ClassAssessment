//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// MainWindow的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region 声明

        //基础设定
        /// <summary>
        /// 绘制接合线的厚度
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// 夹边矩形的厚度 
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// 不知道什么意思
        /// Constant for clamping Z values of camera space points from being negative
        /// 将相机空间点的Z值从负值夹持的常量 
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        //手部设定
        /// <summary>
        /// 绘制的手圆半径 
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// 用于绘制当前跟踪为闭合的手的画笔：红色圆=闭合  
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// 用于绘制当前跟踪为打开的手的画笔：绿色圆=打开 
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// 用于绘制当前被跟踪为套索（指针）位置的手的画笔：蓝色圆=套索 
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        //关节设定
        /// <summary>
        /// 用于绘制当前跟踪的关节的画笔 
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// 用于绘制当前推断的关节的画笔 
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        //骨骼设定
        /// <summary>
        /// 用于绘制当前推断的骨骼的笔 
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// 坐标映射器将一种类型的点映射到另一种类型的点
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// 骨骼帧接收器
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// 识别人体数组
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// 骨骼的定义
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// 骨骼跟踪的颜色列表
        /// </summary>
        private List<Pen> bodyColors;

        //图像输出
        /// <summary>
        /// 用于骨骼绘制输出的绘图组 
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// 我们将要显示的绘图图像 
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// 显示宽度（深度空间） 
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// 显示高度（深度空间） 
        /// </summary>
        private int displayHeight;

        //实例化
        /// <summary>
        /// Kinect实例化
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// 显示当前状态
        /// </summary>
        private string statusText = null;
        #endregion

        /// <summary>
        /// MainWindow实例化
        /// </summary>
        public MainWindow()
        {
            // 将默认的Kinect作为实例化的Kinect
            this.kinectSensor = KinectSensor.GetDefault();

            // 获取坐标映射器 
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // 获取深度（显示）范围 
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // 获取关节空间大小 
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // 初始化目前所用的Kinect的骨骼帧阅读器
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // 定义两个关节点之间的骨头（实际上是一条线）
            this.bones = new List<Tuple<JointType, JointType>>();

            // 人体躯干 
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // 右臂 
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // 左臂 
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // 填充人体颜色，每个人体填充一个颜色 
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // 当Kinect状态更改时，激发IsAvailableChanged事件，调用Sensor_IsAvailableChanged函数判断Kinect当前状态
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // 启动Kinect
            this.kinectSensor.Open();

            // 设置Kinect的状态
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // 创建将用于绘图的绘图组 
            this.drawingGroup = new DrawingGroup();

            // 创建可以在图像控件中使用的图像源 
            this.imageSource = new DrawingImage(this.drawingGroup);

            // 在这个简单的例子中，使用window对象作为视图模型，不知道什么意思
            this.DataContext = this;

            // 初始化窗口的组件（控件） 
            this.InitializeComponent();
        }

        /// <summary>
        /// InotifyPropertyChangedPropertyChanged事件允许窗口控件绑定到可更改的数据 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 展示彩色图像
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// 设置或返回当前Kinect的状态
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
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// 执行开机任务
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// 执行关机任务
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        // 重要！！！已经添加了待修改的注释！！！
        /// <summary>
        /// 处理来自传感器的骨骼帧数据
        /// </summary>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            // 接收骨骼帧
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                // 如果接收骨骼帧成功
                if (bodyFrame != null)
                {
                    // 如果识别到的人体不为空
                    if (this.bodies == null)
                    {
                        // 生成与识别到的人体数等长的数组
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // 第一次调用GetAndrefreshBodyData时，Kinect将在数组中分配每个主体
                    // 只要这些body对象未被释放并且在数组中未设置为空,这些body将被重新使用
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }
            if (dataReceived)
            {
                // DrawingContext：使用绘图描述可视内容
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // 绘制透明背景以设置渲染大小 
                    // DrawRectangle：绘制一个指定的矩形
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    // 对每个识别到的人体进行描绘，包括关节点、骨骼
                    foreach (Body body in this.bodies)
                    {
                        // 对识别到的人体，进行涂色
                        // Pen：描述如何绘制形状轮廓
                        Pen drawPen = this.bodyColors[penIndex++];

                        // 对骨骼被跟踪的人体，进行骨骼数据处理
                        if (body.IsTracked)
                        {
                            // 骨骼点坐标文件保存路径
                            string filePath = @"C:\Users\Administrator\Downloads\kinect_samples\BodyBasics-WPF\ske.txt";

                            // 打开一个已存在的txt文件，不覆盖原文件
                            FileStream file = new FileStream(filePath, FileMode.Append);
                            StreamWriter fileWrite = new StreamWriter(file);

                            // 不知道做什么的
                            this.DrawClippedEdges(body, dc);

                            // 初始化一个只读字典joints，表示关节点的标号和名称
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // 将三维关节点转换二维关节点所用的字典jointPoints
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            // 对关节点坐标的处理
                            // 通过枚举类型jointType，从字典joints中取值
                            // jointType是关节点的名称，joints.Keys也是关节点的名称
                            foreach (JointType jointType in joints.Keys)
                            {
                                // 设置一个continue，跳过下半身关节点的图像输出
                                if ( jointType == JointType.HipLeft ||
                                     jointType == JointType.KneeLeft ||
                                     jointType == JointType.AnkleLeft ||
                                     jointType == JointType.FootLeft ||
                                     jointType == JointType.HipLeft ||
                                     jointType == JointType.KneeRight ||
                                     jointType == JointType.AnkleRight ||
                                     jointType == JointType.FootRight)
                                {
                                    continue;
                                }

                                
                                // joints[jointType]返回一个Joint结构
                                // position：存放关节点的三维坐标
                                CameraSpacePoint position = joints[jointType].Position;

                                // 从txt文件上一次结束的地方开始                               
                                fileWrite.BaseStream.Seek(0, SeekOrigin.End);
                                // 存放X、Y、Z坐标，中间用空格隔开，最后用tap键隔开
                                fileWrite.Write("{0} {1} {2} \t", position.X.ToString(), position.Y.ToString(), position.Z.ToString());

                                // 有时，深度（z）可能显示为负值
                                // 设置下限为0.1f，以防止CoordinateMapper返回（-Infinity，-Infinity）
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                // 将三维坐标转换为深度坐标
                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                // 得到二维坐标
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            // 设置一个语句，让txt文件换行
                            fileWrite.Write("\n");
                            // 关闭文件流
                            fileWrite.Flush();
                            fileWrite.Close();
                            file.Close();

                            // 绘制人体图像
                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            // 绘制双手图像
                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);                            
                        }
                    }
                    // 防止在渲染区域之外绘制 
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// 绘制人体图像
        /// </summary>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // 骨骼绘画
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // 对字典joints里面的每一个关节点，都操作一次
            foreach (JointType jointType in joints.Keys)
            {
                // 设置一个continue，跳过下半身的图像输出
                if (jointType == JointType.HipLeft ||
                     jointType == JointType.KneeLeft ||
                     jointType == JointType.AnkleLeft ||
                     jointType == JointType.FootLeft ||
                     jointType == JointType.HipLeft ||
                     jointType == JointType.KneeRight ||
                     jointType == JointType.AnkleRight ||
                     jointType == JointType.FootRight)
                {
                    continue;
                }

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
        /// 骨骼关节点绘画
        /// </summary>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // 如果找不到关节点就退出 
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // 我们假设所有绘制的骨骼都是推断出来的，除非两个关节都被跟踪
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// 如果跟踪手，则绘制手符号：红色圆=闭合，绿色圆=打开；蓝色圆=套索 
        /// </summary>
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

        /// <summary>
        /// 绘制指示器以显示哪些边正在剪切实体数据 
        /// </summary>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// 处理传感器不可用的事件（例如暂停、关闭、拔出）。 
        /// </summary>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
