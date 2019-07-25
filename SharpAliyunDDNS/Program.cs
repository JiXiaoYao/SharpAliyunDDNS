using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;

namespace SharpAliyunDDNS
{

    class Program
    {
        public static Object locker = new Object();
        public static string LocalIP { set; get; }
        public static bool DDNSState { set; get; }
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddCommandLine(args);
            var configuration = builder.Build();
            string[] ConfigFile;
            if (configuration["config"] != null)
            {
                ConsoleX.WriteLine("开始读取配置文件:" + configuration["config"]);
                if (File.Exists(configuration["config"]))
                {
                    ConfigFile = File.ReadAllLines(configuration["config"]);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    ConsoleX.WriteLine("配置文件不存在!");
                    Console.ResetColor();
                    return;
                }
            }
            else
            {
                ConsoleX.WriteLine("开始读取配置文件:" + Path.GetFullPath("config.json")); ;
                if (File.Exists("config.json"))
                {
                    ConfigFile = File.ReadAllLines("config.json");
                }
                else
                {
                    ConsoleX.WriteLine($"配置文件{{{Directory.GetCurrentDirectory() + "/config.json"}}}不存在!");
                    return;
                }
                if (File.Exists(AppContext.BaseDirectory + "config.json"))
                {
                    ConfigFile = File.ReadAllLines(AppContext.BaseDirectory + "config.json");
                }
                else
                {
                    ConsoleX.WriteLine($"配置文件{{{AppContext.BaseDirectory + "config.json"}}}不存在!");
                    return;
                }
            }
            ConsoleX.WriteLine("配置文件读取完毕");
            DDNSState = true;
            string AccessKeyId = "";
            string AccessKeySecret = "";
            string ConfigContent = "";
            ConsoleX.WriteLine("屏蔽注释中.....");
            foreach (var Da in ConfigFile)
                if (Da.Trim().First() != '/')
                    ConfigContent += Da.Trim();
            string pattern = "[\u4e00-\u9fbb]";
            if (Regex.IsMatch(ConfigContent, pattern))
            {
                ConsoleX.WriteLine("config文件中出现了中文字符串，请检查，本软件暂时不支持中文域名");
                goto ccLever;
            }
            ConsoleX.WriteLine("反序列化解析配置数据");
            Config config = JsonConvert.DeserializeObject<Config>(ConfigContent);
            List<string> DomainConfig = config.data.ToList();
            ConsoleX.WriteLine("获取域名配置成功!");
            foreach (string domain in DomainConfig)
            {
                ConsoleX.WriteLine(domain);
            }
            ConsoleX.WriteLine("载入AccessKeyId/AccessKeySecret数据");
            AccessKeyId = config.key.AccessKeyId;
            AccessKeySecret = config.key.AccessKeySecret;
            string patten = @"(?<=\S)\S(?=\S)";
            Regex reg = new Regex(patten);
            ConsoleX.WriteLine("AccessKeyId:" + reg.Replace(AccessKeyId, "*"));
            ConsoleX.WriteLine("AccessKeySecret:" + reg.Replace(AccessKeySecret, "*"));
            ConsoleX.WriteLine("开始启动DDNS");
            ConsoleX.WriteLine("开始初始化本机公网地址获取函数");
            LocalIP = "0.0.0.0";
            ConsoleX.WriteLine("开始第一次获取公网地址：");
            try { LocalIP= GetlocalhostIP(); } catch (Exception e) { ConsoleX.WriteLine("IP获取发生错误：\r\n" + e.Message); LocalIP = "Error"; }
            ConsoleX.WriteLine("IP:" + LocalIP);
            Thread GetIp = new Thread(new ThreadStart(() =>
            {
                for (int i = 0; i < 60; i++)
                {
                    if (Program.DDNSState)
                        Thread.Sleep(1000);
                    else
                        i = 60;
                }
                while (DDNSState)
                {
                    try { LocalIP = GetlocalhostIP(); } catch (Exception e) { ConsoleX.WriteLine("IP获取发生错误：1分钟后重试\r\n" + e.Message, "IP获取线程"); }
                    for (int i = 0; i < 60; i++)
                    {
                        if (Program.DDNSState)
                            Thread.Sleep(1000);
                        else
                            i = 60;
                    }
                }
            }));
            GetIp.Start();
            List<DDNS> DdnsList = new List<DDNS>();
            for (int i = 0; i < DomainConfig.Count; i++)
            {
                DDNS ddns = new DDNS();
                ddns.InitializeDict(AccessKeyId, AccessKeySecret, DomainConfig[i].Split(',')[0], DomainConfig[i].Split(',')[1], DomainConfig[i].Split(',')[2]);
                ddns.Start();
                Thread.Sleep(1000 * 3);
            }
            Console.WriteLine("防手残机制：如果想结束程序请输入Stop");
        cc: string Read = Console.ReadLine().ToLower();
            if (Read != "stop")
                goto cc;
            DDNSState = false;
        ccLever: Console.WriteLine("回车离开");
            Console.ReadLine();
        }
        public static string GetlocalhostIP()
        {
            string url = "https://api-ipv4.ip.sb/jsonip";
            string IP = "Error";
            HttpClient client = new HttpClient();
            try
            {
                HttpResponseMessage response = client.PostAsync(url, null).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                IPJson iPJson = JsonConvert.DeserializeObject<IPJson>(responseBody);
                if (!iPJson.ip.Equals("")) IP = iPJson.ip;
                else IP = "Error";
            }
            catch (HttpRequestException e)
            { ConsoleX.WriteLine("IP获取发生错误", $"IP获取函数 报错\r\nMessges:{e.Message}"); }
            return IP;
        }
        public class IPJson
        {
            public string ip { set; get; }
        }
    }
    public class ConsoleX
    {
        public static void WriteLine(string Content, string OutObject = "Console")
        {
            // Program.logger.Info(/*"[" + OutObject + " " + DateTime.Now.ToLongTimeString() + "] :" +*/ Content);
            lock (Program.locker)
            {
                Console.Write("[");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(OutObject);
                Console.ResetColor();
                Console.WriteLine(" " + DateTime.Now.ToLongTimeString() + "] :" + Content);
            }
        }
    }
    public class Config
    {
        public string[] data { set; get; }
        public Key key { set; get; }
        public class Key
        {
            public string AccessKeyId { set; get; }
            public string AccessKeySecret { set; get; }
        }
    }
}
