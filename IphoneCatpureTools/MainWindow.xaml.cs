using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
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
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.Screenshotr;
using PublicLibrary;

namespace IphoneCatpureTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 参考文档:
    /// idevice-tools-public https://github.com/PerfectoMobileDev/idevice-tools-public
    /// imobiledevice-net https://github.com/libimobiledevice-win32/imobiledevice-net
    /// libimobiledevice-win64 https://github.com/exaphaser/libimobiledevice-win64
    /// iOS-Info-Kit https://github.com/AjayGhale/iOS-Info-Kit
    /// MK.MobileDevice https://github.com/0xFireball/MK.MobileDevice
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        public static extern bool WriteFile(IntPtr hFile, IntPtr lpBuffer, int NumberOfBytesToWrite, out int lpNumberOfBytesWritten, IntPtr lpOverlapped);

        public MainWindow()
        {
            InitializeComponent();
            NativeLibraries.Load();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            ReadOnlyCollection<string> udids;
            int count = 0;

            var idevice = LibiMobileDevice.Instance.iDevice;
            var lockdown = LibiMobileDevice.Instance.Lockdown;
            var screenshotr=LibiMobileDevice.Instance.Screenshotr;

            var ret = idevice.idevice_get_device_list(out udids, ref count);

            if (ret == iDeviceError.NoDevice)
            {
                // Not actually an error in our case
                Console.WriteLine("No devices found");
                return;
            }

            ret.ThrowOnError();

            // Get the device name
            foreach (var udid in udids)
            {
                iDeviceHandle deviceHandle;
                idevice.idevice_new(out deviceHandle, udid).ThrowOnError();

                LockdownClientHandle lockdownHandle;
                lockdown.lockdownd_client_new_with_handshake(deviceHandle, out lockdownHandle, "Quamotion").ThrowOnError();

                string deviceName;
                lockdown.lockdownd_get_device_name(lockdownHandle, out deviceName).ThrowOnError();


                Console.WriteLine($"{deviceName} ({udid})");

                {
                    LockdownServiceDescriptorHandle scrSvc;
                    LockdownError lde = lockdown.lockdownd_start_service(lockdownHandle, "com.apple.mobile.screenshotr", out scrSvc);
                    if (lde == LockdownError.Success)
                    {
                        ScreenshotrClientHandle client;
                        ScreenshotrError err = screenshotr.screenshotr_client_new(deviceHandle, scrSvc, out client);
                        IntPtr imgData = new IntPtr();
                        ulong imgSize = 0;
                        ScreenshotrError result = screenshotr.screenshotr_take_screenshot(client, ref imgData, ref imgSize);
                        if (result== ScreenshotrError.Success)
                        {
                            Console.WriteLine("截图成功！！！！");
                            String filename = GetScreenPicFileName();
                            Console.WriteLine($"filename:{filename}");

                            System.IO.FileStream file = new System.IO.FileStream(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                            int written;
                            bool writeResult = WriteFile(file.Handle, imgData, (int) imgSize, out written, IntPtr.Zero);
                            file.Close();
                        }
                    }
                    else
                    {
                        Console.WriteLine("截图失败！！！");
                    }
                }

                deviceHandle.Dispose();
                lockdownHandle.Dispose();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private string GetScreenPicFileName()
        {
            string appPath = System.AppDomain.CurrentDomain.BaseDirectory;

            //step1,把图片截取到手机;
            DateTime dateTime = DateTime.Now;
            string formatDate = dateTime.ToString("yyyy_MM_dd_HH_mm_sss");
            string dstFileName = "screen_" + formatDate + ".png";
            string captureDir = appPath + @"\capture";
            FileUtils.CreateDir(captureDir);
            var screenSrcFilePath = appPath + @"\" + dstFileName;//截图地址;
            return screenSrcFilePath;
        }
    }
}
