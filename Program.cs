using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace DomainAvailabilityChecker
{
    class Program
    {
        // 域名后缀列表（只保留长度不超过3个字符的后缀）
        static readonly string[] ShortTlds = {
            // 传统通用顶级域名
            "com", "net", "org", "info", "biz", "pro",
            
            // 国家代码顶级域名
            "us", "uk", "de", "fr", "it", "es", "nl", "ca", "au", "jp", "cn", "in", "ru", "br", "mx",
            "io", "co", "ai", "tv", "me", "cc", "ws", "vg", "je", "gg", "fm", "to", "ms", "nu",
            
            // 新通用顶级域名
            "xyz", "top", "app", "dev", "io", "art", "fit", "fun", "new", "now", "one", "red", "run", "sex", "tax",
            "box", "car", "cat", "dog", "eat", "fly", "gay", "god", "hot", "ink", "job", "law", "men", "mom", "net",
            "pet", "pub", "rip", "sbs", "sky", "soy", "tab", "tel", "top", "vet", "web", "win", "xxx", "yes", "zip",
            
            // 其他短后缀
            "ac", "ad", "ae", "af", "ag", "am", "as", "at", "aw", "ax", "az", "ba", "bb", "bd", "be", "bf", "bg", "bh",
            "bi", "bj", "bm", "bn", "bo", "bq", "bs", "bt", "bw", "by", "bz", "cd", "cf", "cg", "ch", "ci", "ck", "cl",
            "cm", "cr", "cu", "cv", "cw", "cx", "cy", "cz", "dj", "dk", "dm", "do", "dz", "ec", "ee", "eg", "eh", "er",
            "et", "eu", "fi", "fj", "fk", "ga", "gd", "ge", "gf", "gh", "gi", "gl", "gm", "gn", "gp", "gq", "gr", "gt",
            "gu", "gw", "gy", "hk", "hn", "hr", "ht", "hu", "id", "ie", "il", "im", "iq", "ir", "is", "jt", "ke", "kg",
            "kh", "ki", "km", "kn", "kp", "kr", "kw", "ky", "kz", "la", "lb", "lc", "li", "lk", "lr", "ls", "lt", "lu",
            "lv", "ly", "ma", "mc", "md", "mg", "mh", "mk", "ml", "mm", "mn", "mo", "mp", "mq", "mr", "mt", "mu", "mv",
            "mw", "my", "mz", "na", "nc", "ne", "nf", "ng", "ni", "no", "np", "nr", "nz", "om", "pa", "pe", "pf", "pg",
            "ph", "pk", "pl", "pm", "pn", "pr", "ps", "pt", "pw", "py", "qa", "re", "ro", "rs", "rw", "sa", "sb", "sc",
            "sd", "se", "sg", "sh", "si", "sj", "sk", "sl", "sm", "sn", "so", "sr", "ss", "st", "su", "sv", "sx", "sy",
            "sz", "tc", "td", "tf", "tg", "th", "tj", "tk", "tl", "tm", "tn", "tr", "tt", "tw", "tz", "ua", "ug", "uy",
            "uz", "va", "vc", "ve", "vn", "vu", "wf", "ye", "yt", "za", "zm", "zw"
        };

        static async Task Main(string[] args)
        {
            Console.WriteLine("域名注册查询工具");
            Console.WriteLine("==================");

            Console.Write("请输入根域名（不含后缀）: ");
            string rootDomain = Console.ReadLine().Trim();

            if (string.IsNullOrEmpty(rootDomain))
            {
                Console.WriteLine("输入无效，程序结束。");
                return;
            }

            Console.WriteLine($"\n开始查询 {rootDomain} 的注册状态...\n");

            // 存储未注册的域名
            List<string> availableDomains = new List<string>();

            // 并行查询所有后缀，但限制并发数以避免被WHOIS服务器阻止
            var semaphore = new System.Threading.SemaphoreSlim(5, 5); // 限制并发数为5

            var tasks = new List<Task>();
            foreach (var tld in ShortTlds)
            {
                await semaphore.WaitAsync(); // 等待信号量

                string domain = $"{rootDomain}.{tld}";
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        bool isRegistered = await CheckDomainRegistration(domain);
                        if (!isRegistered)
                        {
                            lock (availableDomains)
                            {
                                availableDomains.Add(domain);
                            }
                            Console.WriteLine($"{domain,-20} : 未注册");
                        }
                        else
                        {
                            Console.WriteLine($"{domain,-20} : 已注册");
                        }
                    }
                    finally
                    {
                        semaphore.Release(); // 释放信号量
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // 输出所有未注册的域名
            Console.WriteLine("\n=========================================");
            Console.WriteLine("所有未注册的域名:");
            Console.WriteLine("=========================================");

            if (availableDomains.Any())
            {
                foreach (var domain in availableDomains.OrderBy(d => d))
                {
                    Console.WriteLine(domain);
                }
                Console.WriteLine($"\n共找到 {availableDomains.Count} 个未注册的域名（仅供参考）");
            }
            else
            {
                Console.WriteLine("没有找到未注册的域名");
            }

            Console.WriteLine("\n查询完成！按任意键退出...");
            Console.ReadKey();
        }

        static async Task<bool> CheckDomainRegistration(string domain)
        {
            try
            {
                // 获取该域名的WHOIS服务器
                string whoisServer = GetWhoisServer(domain);
                if (string.IsNullOrEmpty(whoisServer))
                    return true; // 如果找不到WHOIS服务器，假设已注册

                // 连接到WHOIS服务器
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(whoisServer, 43);

                    // 发送查询请求
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] domainBytes = Encoding.ASCII.GetBytes(domain + "\r\n");
                        await stream.WriteAsync(domainBytes, 0, domainBytes.Length);

                        // 读取响应
                        byte[] buffer = new byte[2048];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // 分析响应判断域名是否已注册
                        return IsDomainRegistered(response, domain);
                    }
                }
            }
            catch
            {
                // 如果查询失败，假设域名已注册
                return true;
            }
        }

        static string GetWhoisServer(string domain)
        {
            // 根据域名后缀返回对应的WHOIS服务器
            string tld = domain.Substring(domain.LastIndexOf('.') + 1);

            switch (tld.ToLower())
            {
                case "com":
                case "net":
                    return "whois.verisign-grs.com";
                case "org":
                    return "whois.pir.org";
                case "info":
                    return "whois.afilias.net";
                case "biz":
                    return "whois.neulevel.biz";
                case "io":
                    return "whois.nic.io";
                case "co":
                    return "whois.nic.co";
                case "ai":
                    return "whois.nic.ai";
                case "app":
                case "dev":
                    return "whois.nic.google";
                case "uk":
                    return "whois.nic.uk";
                case "de":
                    return "whois.denic.de";
                case "fr":
                    return "whois.nic.fr";
                case "it":
                    return "whois.nic.it";
                case "es":
                    return "whois.nic.es";
                case "nl":
                    return "whois.domain-registry.nl";
                case "ca":
                    return "whois.cira.ca";
                case "au":
                    return "whois.auda.org.au";
                case "jp":
                    return "whois.jprs.jp";
                case "cn":
                    return "whois.cnnic.cn";
                case "us":
                    return "whois.nic.us";
                default:
                    return $"whois.nic.{tld}";
            }
        }

        static bool IsDomainRegistered(string whoisResponse, string domain)
        {
            // 分析WHOIS响应判断域名是否已注册
            string response = whoisResponse.ToLower();

            // 常见表示未注册的关键词
            if (response.Contains("no match") ||
                response.Contains("not found") ||
                response.Contains("no data found") ||
                response.Contains("available for registration") ||
                response.Contains("domain not found") ||
                response.Contains("no entries found") ||
                response.Contains("status: free") ||
                response.Contains("status: available") ||
                response.Contains("not registered") ||
                response.Contains("no object found"))
            {
                return false;
            }

            // 常见表示已注册的关键词
            if (response.Contains("domain name:") ||
                response.Contains("registrar:") ||
                response.Contains("creation date:") ||
                response.Contains("updated date:") ||
                response.Contains("expiry date:") ||
                response.Contains("name server:") ||
                response.Contains("status: active") ||
                response.Contains("status: registered") ||
                response.Contains("registered") ||
                response.Contains("registrant:") ||
                response.Contains("admin contact:") ||
                response.Contains("tech contact:"))
            {
                return true;
            }

            // 如果没有明确信息，假设已注册
            return true;
        }
    }
}