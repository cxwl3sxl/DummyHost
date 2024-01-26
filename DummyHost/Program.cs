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
                Console.CursorVisible = false;
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
            Console.WriteLine("在程序根目录下的Response目录中存放需要拦截的请求配置，支持子目录");
            Console.WriteLine("该目录下的每个txt文件配置一个拦截请求");
            Console.WriteLine("txt文件第一行表示拦截地址，从/开始");
            Console.WriteLine("空行用于分割返回头");
            Console.WriteLine("返回头内容，支持多行，一行一个，格式为 xxx:xxx");
            Console.WriteLine("空行用于分割返回内容");
            Console.WriteLine("后续内容全部为返回内容");
            Console.WriteLine("详细参考例子.txt");
            File.WriteAllText(Path.Combine(dir, "例子.txt"), @"/api/template
#必须保留一个空行，该行虽然为注释，但不支持注释，注意：只能空一行

content-type:text/html; charset=utf-8
other-header: other-header-value
#必须保留一个空行，该行虽然为注释，但不支持注释，注意：只能空一行

这里是正文了，这里是正文了
这里是正文了，这里是正文了");
        }
    }
}
