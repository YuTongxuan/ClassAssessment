//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// MainWindow交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region 声明       
        /// <summary>
        /// Kinect实例化
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Kinect的状态：开或关
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// 用colorFrameReader接收彩色图像帧
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// 用colorBitmap展示彩色图像帧，且效率更高
        /// </summary>
        private WriteableBitmap colorBitmap = null;      
        #endregion

        /// <summary>
        /// MainWindow实例化
        /// </summary>
        public MainWindow()
        {
            // 初始化窗口的组件（控件） 
            this.InitializeComponent();

            // 将默认的Kinect作为实例化的Kinect
            this.kinectSensor = KinectSensor.GetDefault();

            // 设置Kinect的状态
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // 当Kinect状态更改时，激发IsAvailableChanged事件，调用Sensor_IsAvailableChanged函数判断Kinect当前状态
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
           
            // 初始化目前所用的Kinect的彩色帧阅读器
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            // 当事件FrameArrived发生时，函数Reader_ColorFrameArrived会被调用
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            // FrameDescription：表示来自Kinect传感器的图像帧的属性
            // CreateFrameDescription：返回图片格式，在这里是用了RGB+透明度
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // 把接收的彩色图像帧展示出来
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                           
            // 在这个简单的例子中，使用window对象作为视图模型，不知道啥意思
            this.DataContext = this;

            // 启动Kinect
            this.kinectSensor.Open();
        }

        /// <summary>
        /// InotifyPropertyChangedPropertyChanged事件允许窗口控件绑定到可更改的数据 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 展示彩色图像帧
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        //重点
        /// <summary>
        /// 处理从传感器接收到的颜色帧数据 
        /// </summary>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrameArrivedEventArgs：为事件传递参数，返回当前彩色图像帧
            // ColorFrame：表示某一帧彩色图像帧
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    // FrameDescription：表示来自Kinect传感器的图像帧的属性
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    // 运行Kinect实例访问内存
                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        // 不知道什么意思，可能是对内存的读写控制
                        this.colorBitmap.Lock();

                        // 验证数据并将新的颜色框数据写入显示位图 
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            // 没找到，但我之前见过
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);
                            // 不知道什么意思，估计是画图
                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }
                        // 不知道什么意思，可能是对内存的读写控制
                        this.colorBitmap.Unlock();
                    }
                }
            }
        }

        //还没看，因为我们用不到
        /// <summary>
        /// 处理用户单击屏幕截图按钮的操作 
        /// </summary>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.colorBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = Path.Combine(myPhotos, "KinectScreenshot-Color-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
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
                //看不懂，也不知道value哪里来的
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // 通知任何绑定元素文本已更改
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// 处理传感器不可用的事件（例如暂停、关闭、拔出）。 
        ///  </summary>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// 执行关机任务
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // 没找到，感觉是关闭彩色图像阅读器
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }        
    }
}
