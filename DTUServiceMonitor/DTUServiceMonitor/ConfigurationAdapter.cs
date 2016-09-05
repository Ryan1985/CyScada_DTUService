using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace DTUServiceMonitor
{
    public class ConfigurationAdapter
    {
        private static List<ConfigModel> _syncConfigModel = new List<ConfigModel>();
        private static List<string> _configIndex = new List<string>();
        private static volatile bool IsDirty = true;
        private static volatile bool IsRefreshing;
        private static object m_lock = new object();
        private static Dictionary<string, ConfigModel> configTable = new Dictionary<string, ConfigModel>();
        private static volatile bool IsRunning = true;
        private const int RefreshingRate = 30000;//30秒刷新一次配置


        public static void Initial()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                while (IsRunning)
                {
                    if (!IsRefreshing)
                    {
                        RefreshConfigTable();
                    }
                    Thread.Sleep(RefreshingRate);
                }
            });
        }


        public static Dictionary<string, ConfigModel> GetConfigTable()
        {
            if (IsDirty && !IsRefreshing)
            {
                var tempConfigTable = new Dictionary<string, ConfigModel>();
                ConfigModel[] modelArray;
                string[] indexArray;
                lock (_syncConfigModel)
                {
                    modelArray = new ConfigModel[_syncConfigModel.Count];
                    _syncConfigModel.CopyTo(modelArray);
                    indexArray = new string[_syncConfigModel.Count];
                    _configIndex.CopyTo(indexArray);
                }

                for (var i = 0; i < modelArray.Length; i++)
                {
                    tempConfigTable.Add(indexArray[i], modelArray[i]);
                }

                lock (configTable)
                {
                    configTable = tempConfigTable;
                }
                IsDirty = false;
            }
            return configTable;
        }

        protected static void RefreshConfigTable()
        {
            if (!File.Exists("ConfigFile.txt"))
            {
                throw new FileNotFoundException("未找到配置文件");
            }

            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    if (File.Exists("ConfigFileTemp.txt"))
                    {
                        File.Delete("ConfigFileTemp.txt");
                    }

                    IsRefreshing = true;
                    var syncConfigModel = new List<ConfigModel>();
                    var configIndex = new List<string>();
                    File.Copy("ConfigFile.txt", "ConfigFileTemp.txt");
                    File.SetAttributes("ConfigFileTemp.txt", FileAttributes.Hidden);
                    var strAll = File.ReadAllText("ConfigFileTemp.txt", Encoding.Default);
                    var modelString = strAll.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i < modelString.Length; i++)
                    {
                        var propertyString = modelString[i].Split(new[] {'\t'}, StringSplitOptions.RemoveEmptyEntries);
                        var config = new ConfigModel();
                        var configType = config.GetType();
                        for (var j = 0; j < propertyString.Length; j++)
                        {
                            var proper = configType.GetProperty(ConfigModel.PropertyNameIndex[j]);
                            proper.SetValue(config, Convert.ChangeType(propertyString[j], proper.PropertyType), null);
                        }
                        syncConfigModel.Add(config);
                        configIndex.Add(config.PhoneNo);
                    }

                    lock (m_lock)
                    {
                        _syncConfigModel = syncConfigModel;
                        _configIndex = configIndex;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    File.Delete("ConfigFileTemp.txt");
                    IsRefreshing = false;
                    IsDirty = true;
                }

            });
        }


        public static void Dispose()
        {
            IsRunning = false;
            if (File.Exists("ConfigFileTemp.txt"))
            {
                File.Delete("ConfigFileTemp.txt");
            }
        }
    }
}
