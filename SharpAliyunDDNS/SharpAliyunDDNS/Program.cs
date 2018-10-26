using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace SharpDDNS
{
    class Program
    {
        public static bool DDNSState { set; get; }
        static void Main(string[] args)
        {
            Dictionary<string, string> Configargs = Readargs(args);
            string ConfigPath = null;
            foreach (var Var in Configargs)
            {
                if (Var.Key == "")
                    ConfigPath = Var.Value;
            }
            DDNSState = true;
            string AccessKeyId = "";
            string AccessKeySecret = "";
            ConsoleX.WriteLine("开始读取配置文件");
            string[] ConfigFile;
            if (ConfigPath != "" && ConfigPath != null)
            {
                ConfigFile = File.ReadAllLines(ConfigPath);
            }
            else
            {
                if (File.Exists("Config.json"))
                    ConfigFile = File.ReadAllLines("Config.json");
                else
                    ConfigFile = File.ReadAllLines(AppContext.BaseDirectory + "Config.json");
            }
            string ConfigContent = "";
            ConsoleX.WriteLine("屏蔽注释");
            foreach (var Da in ConfigFile)
                if (Da.First() != '/')
                    ConfigContent += Da;
            string pattern = "[\u4e00-\u9fbb]";
            if (Regex.IsMatch(ConfigContent, pattern))
            {
                Console.WriteLine("config文件中出现了中文字符串，请检查，本软件暂时不支持中文域名");
                goto ccLever;
            }
            ConsoleX.WriteLine("反序列化解析配置数据");
            JObject jsonObj = JObject.Parse(ConfigContent);
            JArray jlist = JArray.Parse(jsonObj["data"].ToString());
            List<string> Config = new List<string>();
            ConsoleX.WriteLine("载入解析配置数据");
            foreach (var jk in jlist)
            {
                Config.Add(jk.ToString());
                Console.WriteLine(jk.ToString());
            }
            ConsoleX.WriteLine("载入AccessKeyId/AccessKeySecret数据");
            JToken record = JObject.Parse(ConfigContent)["Key"];
            foreach (JProperty jp in record)
                if (jp.Name == "AccessKeyId")
                    AccessKeyId = jp.Value.ToString();
                else if (jp.Name == "AccessKeySecret")
                    AccessKeySecret = jp.Value.ToString();
            ConsoleX.WriteLine("开始启动DDNS");
            List<DDNS> DdnsList = new List<DDNS>();
            for (int i = 0; i < Config.Count; i++)
            {
                DDNS ddns = new DDNS();
                ddns.InitializeDict(AccessKeyId, AccessKeySecret, Config[i].Split(',')[0], Config[i].Split(',')[1], Config[i].Split(',')[2]);
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
        public static Dictionary<string, string> Readargs(string[] args)
        {
            Dictionary<string, string> Return = new Dictionary<string, string>();
            foreach (string Var in args)
            {
                if (Var.First() == '-')
                {
                    if (Var.ToCharArray()[1] == 'c')
                        Return.Add("Path", Var.Substring(2));
                }
                else
                {

                }
            }
            return Return;
        }
    }
    public class ConsoleX
    {
        public static void WriteLine(string Content, string OutObject = "Console")
        {
            Console.WriteLine("[" + OutObject + " " + DateTime.Now.ToLongTimeString() + "] :" + Content);
        }
    }
}
