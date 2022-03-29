using System;
using System.IO;
using System.Net;
using PinFun.Core.Net.Common;
using PinFun.Core.Net.Http;

namespace DummyHost
{
    class Program
    {
        static void Main()
        {
            try
            {
                Help();
                var config = new Config();
                config.IpAddress ??= IPAddress.Any;
                var httpServer = TryStartHttp(config);
                var httpsServer = TryStartHttps(config);

                Console.WriteLine("服务已经成功启动，任意键退出...");
                if (httpServer != null)
                {
                    Console.WriteLine($"http://{config.IpAddress}:{config.Port}");
                }

                if (httpsServer != null)
                {
                    Console.WriteLine($"https://{config.IpAddress}:{config.SslPort}");
                }

                Console.WriteLine("--------------");
                Console.ReadKey();
                httpServer?.Stop();
                httpsServer?.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动失败:{Environment.NewLine}{ex}");
            }
        }

        static HttpServer TryStartHttp(Config config)
        {
            try
            {
                var server =
                    new HttpServer(config.Port, "业务模拟服务", false, 10240, 0, config.IpAddress);
                server.ConfigServer(appBuilder => { appBuilder.Use<DummyResponseHandler>(); });
                server.Start().Wait();
                return server;
            }
            catch (Exception ex)
            {
                Console.Write($"HTTP服务启动失败：{ex.Message}");
                return null;
            }
        }

        static HttpServer TryStartHttps(Config config)
        {
            try
            {
                HttpServer sslServer = null;
                if (File.Exists("ssl.pfx"))
                {
                    sslServer = new HttpServer(config.SslPort, "业务模拟服务SSL", false, 10240, 0, config.IpAddress);
                    sslServer.ConfigServer(appBuilder => { appBuilder.Use<DummyResponseHandler>(); });
                    sslServer.SetCertificateInfo(new TlsCertificateInfo()
                    {
                        CertificateFile = "ssl.pfx",
                        IsPasswordEncrypted = false,
                        Password = config.SslPassword
                    });
                    sslServer.Start().Wait();
                }

                return sslServer;
            }
            catch (Exception ex)
            {
                Console.Write($"HTTPS服务启动失败：{ex.Message}");
                return null;
            }
        }

        static void Help()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Response");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Console.WriteLine("在程序根目录下的Response目录中存放拦截请求后的返回值");
            Console.WriteLine("1. 用~替换路径分隔符/，例如：请求a/b/c.txt 将映射到文件 a~b~c.txt 上");
            Console.WriteLine("2. 同样支持文件夹模式，例如：请求a/b/c.txt 也可以映射到 a/b~c.txt 上");
            Console.WriteLine("3. 反馈文件支持设置返回头，换行后为正文，参考 Response/例子.txt");
            Console.WriteLine("4. 由于无后缀文件不会触发更新因此无后缀文件需要手动重启或者添加删除有后缀文件用以触发");
            File.WriteAllText(Path.Combine(dir, "例子.txt"), @"content-type:text/html; charset=utf-8
other-header: other-header-value

这里是正文了，这里是正文了
这里是正文了，这里是正文了");
        }
    }
}
