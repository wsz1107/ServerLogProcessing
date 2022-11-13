using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace ServerLogProcessing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<ServerStatus> ServerStatusList = new List<ServerStatus>();
            List<NetworkStatus> NetworkStatusList = new List<NetworkStatus>();

            ReadServerLog(ServerStatusList, NetworkStatusList);
            
            TaskOne(ServerStatusList);
            Console.WriteLine("Task1 => T1Output.txt");

            const int consecutiveTimeOutCount = 1;
            TaskTwo(ServerStatusList, consecutiveTimeOutCount);
            Console.WriteLine("Task2 => T2Output.txt");

            const int overloadCount = 2;
            const int overloadTime = 100;
            TaskThree(ServerStatusList, overloadCount, overloadTime);
            Console.WriteLine("Task3 => T3Output.txt");

            const int networkConsecutiveTimeOutCount = 2;
            TaskFour(NetworkStatusList, networkConsecutiveTimeOutCount);
            Console.WriteLine("Task4 => T4Output.txt");

            Console.WriteLine("Process completed. Press any key to exit. ");
            Console.ReadLine();
        }

        /// <summary>
        /// ログファイルを読み込む
        /// </summary>
        static public void ReadServerLog(List<ServerStatus> serverList, List<NetworkStatus> networkList)
        {
            foreach (string line in File.ReadLines("testdata.txt"))
            {
                string[] tmpLogData = line.Split(',');
                string tmpTimeStamp = tmpLogData[0];
                string tmpIpAddress = tmpLogData[1].Split('/')[0];
                IPNetwork ipnetwork = IPNetwork.Parse(tmpLogData[1]);
                string tmpIpNetwork = ipnetwork.Network.ToString();
                int tmpIpCidr = ipnetwork.Cidr;
                string tmpPingResult = tmpLogData[2];
                int tmpPingTime;
                if (tmpPingResult == "-")
                {
                    tmpPingTime = -1;
                }
                else
                {
                    tmpPingTime = Int32.Parse(tmpPingResult);
                }
                ServerStatus tmpServerStatus = serverList.FirstOrDefault(x => x.ipAddress == tmpIpAddress);
                if (tmpServerStatus == null)
                {
                    serverList.Add(new ServerStatus(tmpIpAddress));
                    serverList.Last().AddPingRecord(tmpTimeStamp, tmpPingTime);
                }
                else
                {
                    tmpServerStatus.AddPingRecord(tmpTimeStamp, tmpPingTime);
                }

                NetworkStatus tmpNetwork = networkList.FirstOrDefault(x => x.ipNetwork == tmpIpNetwork);
                if (tmpNetwork == null)
                {
                    networkList.Add(new NetworkStatus(tmpIpNetwork, tmpIpCidr));
                }

                List<NetworkStatus> tmpNetworkStatusList = networkList.Where(x => IPNetwork.Parse(x.ipNetwork+"/"+x.cidr).Contains(IPAddress.Parse(tmpIpAddress))).ToList();
                if(tmpNetworkStatusList != null)
                {
                    foreach(NetworkStatus tmpNetworkStatus in tmpNetworkStatusList)
                    {
                        tmpNetworkStatus.AddPingRecord(tmpTimeStamp, tmpPingTime);
                    }
                }
            }
        }
        /// <summary>
        /// 設問1
        /// 監視ログファイルを読み込み、故障状態のサーバアドレスとそのサーバの故障期間を出力するプログラムを作成せよ。
        /// 出力フォーマットは任意でよい。
        /// なお、pingがタイムアウトした場合を故障とみなし、最初にタイムアウトしたときから、次にpingの応答が返るまでを故障期間とする。
        /// </summary>
        /// <param name="serverList"></param>
        static public void TaskOne(List<ServerStatus> serverList)
        {
            using (StreamWriter sw = new StreamWriter("T1Output.txt"))
            {
                foreach (ServerStatus server in serverList)
                {
                    if (server.WriteSimpleTimeOutPeriod() != null)
                    {
                        sw.WriteLine(server.WriteSimpleTimeOutPeriod());
                    }
                }
            }
        }
        /// <summary>
        /// 設問2
        /// ネットワークの状態によっては、一時的にpingがタイムアウトしても、一定期間するとpingの応答が復活することがあり、
        /// そのような場合はサーバの故障とみなさないようにしたい。
        /// N回以上連続してタイムアウトした場合にのみ故障とみなすように、設問1のプログラムを拡張せよ。
        /// Nはプログラムのパラメータとして与えられるようにすること。
        /// </summary>
        /// <param name="serverList"></param>
        /// <param name="timeOutCountLine">N回以上連続のパラメータN</param>
        static public void TaskTwo(List<ServerStatus> serverList, int timeOutCountLine)
        {
            using (StreamWriter sw = new StreamWriter("T2Output.txt"))
            {
                foreach (ServerStatus server in serverList)
                {
                    if (server.WriteComplicatedTimeOutPeriod(timeOutCountLine) != null)
                    {
                        sw.WriteLine(server.WriteComplicatedTimeOutPeriod(timeOutCountLine));
                    }
                }
            }
        }
        /// <summary>
        /// 設問3 
        /// サーバが返すpingの応答時間が長くなる場合、サーバが過負荷状態になっていると考えられる。
        /// そこで、直近m回の平均応答時間がtミリ秒を超えた場合は、サーバが過負荷状態になっているとみなそう。
        /// 設問2のプログラムを拡張して、各サーバの過負荷状態となっている期間を出力できるようにせよ。mとtはプログラムのパラメータとして与えられるようにすること。
        /// </summary>
        /// <param name="serverList"></param>
        /// <param name="overloadCountLine">直近m回のパラメータm</param>
        /// <param name="overloadTimeLine">平均応答時間がtミリ秒を超える場合におけるパラメータt</param>
        static public void TaskThree(List<ServerStatus> serverList, int overloadCountLine, int overloadTimeLine)
        {
            using (StreamWriter sw = new StreamWriter("T3Output.txt"))
            {
                foreach (ServerStatus server in serverList)
                {
                    if (server.WriteOverloadPeriod(overloadCountLine, overloadTimeLine) != null)
                    {
                        sw.WriteLine(server.WriteOverloadPeriod(overloadCountLine, overloadTimeLine));
                    }
                }
            }
        }
        /// <summary>
        /// 設問4
        /// ネットワーク経路にあるスイッチに障害が発生した場合、そのスイッチの配下にあるサーバの応答がすべてタイムアウトすると想定される。
        /// そこで、あるサブネット内のサーバが全て故障（ping応答がすべてN回以上連続でタイムアウト）している場合は、
        /// そのサブネット（のスイッチ）の故障とみなそう。
        /// </summary>
        /// <param name="networkList"></param>
        /// <param name="timeOutCountLine">N回以上連続のパラメータN</param>
        static public void TaskFour(List<NetworkStatus> networkList, int timeOutCountLine)
        {
            using (StreamWriter sw = new StreamWriter("T4Output.txt"))
            {
                foreach (NetworkStatus network in networkList)
                {
                    if (network.WriteComplicatedTimeOutPeriod(timeOutCountLine) != null)
                    {
                        sw.WriteLine(network.WriteComplicatedTimeOutPeriod(timeOutCountLine));
                    }
                }
            }
        }
    }
    /// <summary>
    /// サーバーIPごとにログを保存する
    /// </summary>
    class ServerStatus
    {
        /// <summary>
        /// サーバーのipアドレス
        /// </summary>
        public string ipAddress { get; private set; }

        /// <summary>
        /// key=ログのタイムスタンプ, value=ログのping応答時間
        /// </summary>
        public List<KeyValuePair<string, int>> pingRecordList { get; private set; }

        /// <summary>
        /// ログを追加する
        /// </summary>
        public void AddPingRecord(string timeStamp, int pingTime)
        {
            this.pingRecordList.Add(new KeyValuePair<string,int>(timeStamp, pingTime));
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ServerStatus(string ipAddress)
        {
            this.ipAddress = ipAddress;
            pingRecordList = new List<KeyValuePair<string,int>>();
        }

        /// <summary>
        /// タイムアウトごとに記録を出力
        /// </summary>
        public string WriteSimpleTimeOutPeriod()
        {
            bool hasEverTimeOut = false;
            bool hasTimeOutStarted = false;
            string outputLog = String.Format("故障サーバーアドレス：{0}\n",ipAddress);
            foreach(KeyValuePair<string,int> record in pingRecordList)
            {
                if(record.Value < 0)
                {
                    if(!hasTimeOutStarted)
                    {
                        hasTimeOutStarted = true;
                        string startTimeLog = String.Format("故障開始時間：{0}\n", timeFormat(record.Key));
                        outputLog += startTimeLog;
                        hasEverTimeOut = true;
                    }
                }
                else
                {
                    if(hasTimeOutStarted)
                    {
                        string endTimeLog = String.Format("故障停止時間：{0}\n", timeFormat(record.Key));
                        outputLog += endTimeLog;
                        hasTimeOutStarted = false;
                    }
                }
            }
            if (hasEverTimeOut)
            {
                return outputLog;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 連続N回以上のタイムアウトの場合、開始時間と終了時間を出力
        /// </summary>
        public string WriteComplicatedTimeOutPeriod(int timeOutLine)
        {
            int timeOutCount = 0;
            bool hasEverTimeOut = false;
            bool hasTimeOutStarted = false;
            string outputLog = String.Format("故障サーバーアドレス：{0}\n", ipAddress);
            foreach(KeyValuePair<string, int> record in pingRecordList)
            {
                if (record.Value < 0)
                {
                    timeOutCount++;
                    if(timeOutCount>= timeOutLine)
                    {
                        if (!hasTimeOutStarted)
                        {
                            hasTimeOutStarted = true;
                            string startTimeLog = String.Format("故障開始時間：{0}\n", timeFormat(record.Key));
                            outputLog += startTimeLog;
                            hasEverTimeOut = true;
                        }
                    }
                }
                else
                {
                    if(timeOutCount < timeOutLine)
                    {
                        timeOutCount = 0;
                    }
                    if (hasTimeOutStarted)
                    {
                        string endTimeLog = String.Format("故障停止時間：{0}\n", timeFormat(record.Key));
                        outputLog += endTimeLog;
                        hasTimeOutStarted = false;
                    }
                }
            }
            if (hasEverTimeOut)
            {
                return outputLog;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// m回ping応答時間がtミリ秒を超えた場合、開始時間と終了時間を出力
        /// </summary>
        public string WriteOverloadPeriod(int overloadCountLine, int overloadTimeLine)
        {
            bool hasEverOverload = false;
            bool hasOverloadStarted = false;
            string outputLog = String.Format("サーバーアドレス：{0}\n", ipAddress);
            for(int i= overloadCountLine-1;i< pingRecordList.Count; i++)
            {
                if(aveOfPingTime(i, overloadCountLine)> overloadTimeLine)
                {
                    if (!hasOverloadStarted)
                    {
                        hasEverOverload = true;
                        hasOverloadStarted = true;
                        string startTimeLog = String.Format("過負荷開始時間：{0}\n", timeFormat(pingRecordList[i].Key));
                        outputLog += startTimeLog;
                    }
                }
                else if(hasOverloadStarted)
                {
                    hasOverloadStarted = false;
                    string endTimeLog = String.Format("過負荷終了時間：{0}\n", timeFormat(pingRecordList[i].Key));
                    outputLog += endTimeLog;
                }
            }
            if (hasEverOverload)
            {
                return outputLog;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 時間をフォーマットする
        /// </summary>
        private string timeFormat(string time)
        {
            string year = time.Substring(0, 4);
            string month = time.Substring(4,2);
            string day = time.Substring(6,2);
            string hour = time.Substring(8,2);
            string minute = time.Substring(10,2);
            string second = time.Substring(12,2);
            string tmpFormatTime = string.Format("{0}年{1}月{2}日{3}:{4}:{5}", year, month, day, hour, minute, second);
            return tmpFormatTime;
        }

        /// <summary>
        /// startInd個目のログからcount個前までの応答時間の平均値を計算する
        /// </summary>
        private int aveOfPingTime(int startInd, int count)
        {
            int sum = 0;
            int avaliablePingCount=count;
            int ave = 0;
            for(int i=startInd;i> startInd-count; i--)
            {
                if(pingRecordList[i].Value != -1)
                {
                    sum += pingRecordList[i].Value;
                }
                else
                {
                    avaliablePingCount--;
                }
            }
            if (avaliablePingCount != 0)
            {
                ave = sum / avaliablePingCount;
            }
            return ave;
        }
    }

    class NetworkStatus
    {
        /// <summary>
        /// ネットワーク部
        /// </summary>
        public string ipNetwork { get; private set; }

        /// <summary>
        /// サブネットマスク
        /// </summary>
        public int cidr { get; private set; }

        /// <summary>
        /// key=ログのタイムスタンプ, value=ログのping応答時間
        /// </summary>
        public List<KeyValuePair<string, int>> pingRecordList { get; private set; }

        /// <summary>
        /// ログを追加する
        /// </summary>
        public void AddPingRecord(string timeStamp, int pingTime)
        {
            this.pingRecordList.Add(new KeyValuePair<string, int>(timeStamp, pingTime));
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NetworkStatus(string ipNetwork, int cidr)
        {
            this.ipNetwork = ipNetwork;
            this.cidr = cidr;
            pingRecordList = new List<KeyValuePair<string, int>>();
        }

        /// <summary>
        /// 連続N回以上のタイムアウトの場合、開始時間と終了時間を出力
        /// </summary>
        public string WriteComplicatedTimeOutPeriod(int timeOutCountLine)
        {
            int timeOutCount = 0;
            bool hasEverTimeOut = false;
            bool hasTimeOutStarted = false;
            string outputLog = String.Format("故障ネットワークアドレス：{0}/{1}\n", ipNetwork,cidr);
            foreach (KeyValuePair<string, int> record in pingRecordList)
            {
                if (record.Value < 0)
                {
                    timeOutCount++;
                    if (timeOutCount >= timeOutCountLine)
                    {
                        if (!hasTimeOutStarted)
                        {
                            hasTimeOutStarted = true;
                            string startTimeLog = String.Format("故障開始時間：{0}\n", timeFormat(record.Key));
                            outputLog += startTimeLog;
                            hasEverTimeOut = true;
                        }
                    }
                }
                else
                {
                    if (timeOutCount < timeOutCountLine)
                    {
                        timeOutCount = 0;
                    }
                    if (hasTimeOutStarted)
                    {
                        string endTimeLog = String.Format("故障停止時間：{0}\n", timeFormat(record.Key));
                        outputLog += endTimeLog;
                        hasTimeOutStarted = false;
                    }
                }
            }
            if (hasEverTimeOut)
            {
                return outputLog;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 時間をフォーマットする
        /// </summary>
        private string timeFormat(string time)
        {
            string year = time.Substring(0, 4);
            string month = time.Substring(4, 2);
            string day = time.Substring(6, 2);
            string hour = time.Substring(8, 2);
            string minute = time.Substring(10, 2);
            string second = time.Substring(12, 2);
            string tmpFormatTime = string.Format("{0}年{1}月{2}日{3}:{4}:{5}", year, month, day, hour, minute, second);
            return tmpFormatTime;
        }
    }
}
