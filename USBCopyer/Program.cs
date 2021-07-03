using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

using static USBCopyer.Utils.MyLogger;

// ReSharper disable LocalizableElement

namespace USBCopyer
{
    internal static class Program
    {
        public static bool showIcon = true;
        public static System.Drawing.Icon ico = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [MTAThread]
        private static void Main(string[] args)
        {
            try
            {
                Application.SetCompatibleTextRenderingDefault(false);

                if (!System.IO.Directory.Exists(Host.confdir))
                    System.IO.Directory.CreateDirectory(Host.confdir);

                var useUglyUi = false;
                foreach (var arg in args)
                {
                    var argLower = arg.ToLower();
                    switch (argLower)
                    {
                        case "/hide":
                        case "-hide":
                            showIcon = false;
                            break;

                        case "/gui":
                        case "-gui":
                            showIcon = true;
                            break;

                        case "/setting":
                        case "-setting":
                            var th = new Thread(() =>
                            {
                                Application.Run(new Setting());
                            });
                            th.SetApartmentState(ApartmentState.STA);
                            th.Start();
                            break;

                        case "/reset":
                        case "-reset":
                            try
                            {
                                Properties.Settings.Default.Reset();
                                Properties.Settings.Default.Save();
                                Environment.Exit(0);
                            }
                            catch (Exception)
                            {
                                Environment.Exit(1);
                            }
                            break;

                        case "/uglyui":
                        case "-uglyui":
                            useUglyUi = true;
                            break;
                    }
                }

                if (!useUglyUi)
                    Application.EnableVisualStyles();

                //设置应用程序处理异常方式：ThreadException处理
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic);
                //处理UI线程异常
                Application.ThreadException += (sender, e) =>
                {
                    Log($"程序遇到未处理的异常：{e.Exception.Message}");
                };

                //处理非UI线程异常
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Log($"程序遇到未处理的异常：{e.ExceptionObject}");
                };

                Application.Run(new Host());
            }
            catch (Exception ex)
            {
                Log($"程序遇到致命异常：{ex.Message}", LogType.Error);
            }
        }




        /// <summary>
        /// 判断自身是否为管理员权限
        /// </summary>
        /// <returns>是否为管理员权限</returns>
        public static bool IsAdminPermission()
        {
            //获得当前登录的Windows用户标示
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            //判断当前登录用户是否为管理员
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// 检查自身是否为管理员权限，并尝试索取
        /// </summary>
        /// <returns></returns>
        public static bool CheckAdminPermission(string command = "")
        {
            if (IsAdminPermission()) return true;

            if (MessageBox.Show($"该操作需要管理员权限才能完成，是否立即以管理员权限重启{Application.ProductName}?", "需要管理员权限",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return false;
                
            //创建启动对象
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Application.ExecutablePath,
                Arguments = command,
                //设置启动动作,确保以管理员身份运行
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
                Environment.Exit(140);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败。要继续操作，请先退出，然后以管理员权限运行本程序。\r\n{ex.Message}", "需要管理员权限", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
            return false;

        }
    }
}
