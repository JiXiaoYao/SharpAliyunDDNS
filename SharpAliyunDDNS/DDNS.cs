using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace SharpAliyunDDNS
{
    public class DDNS
    {
        private string AccessKeyId { set; get; }
        private string AccessKeySecret { set; get; }
        private string DomainName { set; get; }
        private string HostRecord { set; get; }
        private string TTL { set; get; }
        private string RecordId { set; get; }
        private string LastIP { set; get; }
        public bool State { set; get; }
        public Thread thread { set; get; }
        public DDNS()
        {
            ConsoleX.WriteLine("程序启动，DDNS类已经初始化", "DDNS");
        }

        public void InitializeDict(string AccessKeyId, string AccessKeySecret, string DomainName, string HostRecord, string TTL)
        {
            this.AccessKeyId = AccessKeyId;
            this.AccessKeySecret = AccessKeySecret;
            this.DomainName = DomainName;
            this.HostRecord = HostRecord;
            this.TTL = TTL;
        }
        public string Select()
        {
            GetPostString.RequestString requestString = new GetPostString.RequestString();
            requestString.InitializeDict(AccessKeyId);
            requestString.DictData.Add("Action", "DescribeDomainRecords");
            requestString.DictData.Add("DomainName", DomainName);
            requestString.Signature(AccessKeySecret);
            string HttpGetString = requestString.Serialization();
            string R = CreateGetHttpResponse("http://alidns.aliyuncs.com/?" + HttpGetString);
            JObject obj = JObject.Parse(R);
            string IP = "";
            try { IP = obj.SelectToken("$.DomainRecords.Record[?(@.RR == '" + HostRecord + "')].Value").Value<string>(); } catch { IP = null; goto cc; }
            RecordId = obj.SelectToken("$.DomainRecords.Record[?(@.RR == '" + HostRecord + "')].RecordId").Value<string>();
        cc: return IP;
        }
        public void Add(string IP)
        {
            GetPostString.RequestString requestString = new GetPostString.RequestString();
            requestString.InitializeDict(AccessKeyId);
            requestString.DictData.Add("Action", "AddDomainRecord");
            requestString.DictData.Add("DomainName", DomainName);
            requestString.DictData.Add("RR", HostRecord);
            requestString.DictData.Add("Type", "A");
            requestString.DictData.Add("Value", IP);
            requestString.DictData.Add("TTL", TTL);
            requestString.DictData.Add("Line", "default");
            requestString.Signature(AccessKeySecret);
            string HttpGetString = requestString.Serialization();
            string Return = CreateGetHttpResponse("http://alidns.aliyuncs.com/?" + HttpGetString);
        }
        public void Update(string IP)
        {
            GetPostString.RequestString requestString = new GetPostString.RequestString();
            requestString.InitializeDict(AccessKeyId);
            requestString.DictData.Add("Action", "UpdateDomainRecord");
            requestString.DictData.Add("RecordId", RecordId);
            requestString.DictData.Add("RR", HostRecord);
            requestString.DictData.Add("Type", "A");
            requestString.DictData.Add("Value", IP);
            requestString.DictData.Add("TTL", TTL);
            requestString.Signature(AccessKeySecret);
            string HttpGetString = requestString.Serialization();
            string Return = CreateGetHttpResponse("http://alidns.aliyuncs.com/?" + HttpGetString);
        }
        public void Start()
        {
            //ConsoleX.WriteLine("");
            State = true;
            LastIP = "Null";
            Thread thread = new Thread(new ThreadStart(() =>
            {
            reset: ConsoleX.WriteLine("域名：" + HostRecord + "." + DomainName + "的DDNS已启动");
                ConsoleX.WriteLine("开始第一次查询IP", HostRecord + "." + DomainName);
                string IP = "";
                if (Program.LocalIP != "Error")
                {
                    IP = Program.LocalIP;
                }
                while (Program.DDNSState)
                {
                    if (Program.LocalIP != "Error") { IP = Program.LocalIP; }
                    else { ConsoleX.WriteLine("IP获取发生错误，一分钟后即将重试", HostRecord + "." + DomainName); GC.Collect(); goto rewait; }
                    if (IP != LastIP)
                    {
                        try
                        {
                            ConsoleX.WriteLine("已获取IP ：" + IP + "与上次获取的IP：" + LastIP + "不符，开始更新DNS", HostRecord + "." + DomainName);
                            LastIP = IP;
                            string DnsIP = null;
                            ConsoleX.WriteLine("开始查询域名解析记录", HostRecord + "." + DomainName);
                            try { DnsIP = Select(); }
                            catch
                            {
                                ConsoleX.WriteLine("未获取到解析记录，准备添加解析记录", HostRecord + "." + DomainName); goto cc;
                            }
                        cc: if (DnsIP == null)
                            {
                                ConsoleX.WriteLine("未获取到解析记录，准备添加解析记录", HostRecord + "." + DomainName);
                                try { Add(IP); } catch (Exception e) { ConsoleX.WriteLine("IP添加解析记录发生错误:" + e.Message, HostRecord + "." + DomainName); }
                                ConsoleX.WriteLine("解析记录添加完毕", HostRecord + "." + DomainName);
                            }
                            else if (DnsIP != IP)
                            {
                                ConsoleX.WriteLine("已获取解析记录:" + DnsIP + " 与本机IP:" + IP + "不符，开始更新解析记录", HostRecord + "." + DomainName);
                                try { Update(IP); } catch { goto reset; }
                                ConsoleX.WriteLine("解析记录更新完毕", HostRecord + "." + DomainName);
                            }
                            else
                            {
                                ConsoleX.WriteLine("已获取解析记录:" + DnsIP + " 与本机IP:" + IP + "相符，不进行操作", HostRecord + "." + DomainName);
                            }
                        }
                        catch (Exception e)
                        {//Message
                            JToken record = JObject.Parse(e.Message);
                            string Error = "";
                            foreach (JProperty jp in record)
                                if (jp.Name == "Message")
                                    Error = jp.Value.ToString();
                            Console.Write("[" + HostRecord + "." + DomainName + " ");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(DateTime.Now.ToLongTimeString() + " Error]");
                            Console.ResetColor();
                            Console.Write("发生访问错误，请检查阿里云Key以及域名、主机记录是否正常，异常多数发生在，阿里云Key以及域名或者TTL过低，如果并未专门购买阿里云云解析的TTL值，请将ttl设为600\r\n这是服务器返回数据：\r\n        ");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(Error + "\r\n");
                            Console.ResetColor();
                        }
                    }
                rewait: for (int i = 0; i < 60; i++)
                    {
                        if (Program.DDNSState)
                            Thread.Sleep(1000);
                        else
                            i = 60;
                    }
                }
            }));
            thread.Start();
            ConsoleX.WriteLine("DDNS线程已启动", HostRecord + "." + DomainName);
        }
        public void Stop()
        {
            thread.Abort();
        }



        public static string CreateGetHttpResponse(string url)
        {
            string result = string.Empty;
            HttpClient client = new HttpClient();
            try
            {
                HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ConsoleX.WriteLine("发生错误：" + ex.Message, "HTTP");
                throw;
            }
            return result;
        }
    }
    public class GetPostString
    {
        public class RequestString
        {
            public SortedDictionary<string, string> DictData;
            /// <summary>
            /// 初始化字典
            /// </summary>
            /// <param name="AccessKeyId"></param>
            public void InitializeDict(string AccessKeyId)
            {
                DictData = new SortedDictionary<string, string>(StringComparer.Ordinal) {
                {"Format", "json" },
                {"AccessKeyId", AccessKeyId },
                {"SignatureMethod", "HMAC-SHA1" },
                {"SignatureVersion", "1.0" },
                {"SignatureNonce", Guid.NewGuid().ToString() },
                {"Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                { "Version", "2015-01-09"}
                };

            }
            /// <summary>
            /// 对当前对象内的字典进行序列化
            /// </summary>
            /// <returns></returns>
            public string Serialization()
            {
                string Return = "";
                foreach (var kvm in DictData)
                {
                    Return += "&" +
                        HttpUtility.UrlEncode(kvm.Key) + "=" +
                        HttpUtility.UrlEncode(kvm.Value);
                }
                return Return.Substring(1).Replace("%253a", "%253A").Replace("%2b", "%2B").Replace("%3d", "%3D").Replace("%2f", "%2F");
            }
            /// <summary>
            /// 结合Key数据以及对象内的字典进行签名
            /// </summary>
            /// <param name="AccessKeySecret">阿里云签名秘钥</param>
            /// <param name="HttpMethod">http格式：GET或者Post</param>
            /// <returns></returns>
            public string Signature(string AccessKeySecret, string HttpMethod = "GET")
            {
                string RawString = Serialization();
                HMACSHA1 HmacSha1 = new HMACSHA1(Encoding.UTF8.GetBytes(AccessKeySecret + "&"));
                string Data = HttpMethod + "&" + HttpUtility.UrlEncode("/") + "&" + HttpUtility.UrlEncode(RawString);
                string Singer = Convert.ToBase64String(HmacSha1.ComputeHash(Encoding.UTF8.GetBytes(Data.Replace("%253a", "%253A").Replace("%2b", "%2B").Replace("%3d", "%3D").Replace("%2f", "%2F"))));
                DictData.Add("Signature", Singer);
                return Singer;
            }
        }

    }
}
