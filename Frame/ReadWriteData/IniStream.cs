using Machine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMachine.Frame.ReadWriteData
{
    public class IniStream
    {
        internal System.Threading.ReaderWriterLockSlim FileLock { get => fileLock; }

        #region // 字段

        //CustomLog log;

        /// <summary>
        /// 文件锁
        /// </summary>
        System.Threading.ReaderWriterLockSlim fileLock;
        /// <summary>
        /// 文件名
        /// </summary>
        string fileName;
        /// <summary>
        /// 对应文件中的 节-键-值
        /// </summary>
        Dictionary<string, Dictionary<string, string>> fileMap;

        #endregion

        #region // 文件操作方法

        /// <summary>
        /// 打开文件并读取文件内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool OpenRead(string filePath)
        {
            try
            {
                this.fileLock = new System.Threading.ReaderWriterLockSlim();
                this.fileName = filePath;
                string data = "";

                // 创建读取流读取全部数据
                using (StreamReader reader = new StreamReader(new FileStream(fileName, FileMode.OpenOrCreate)))
                {
                    data = reader.ReadToEnd();
                }

                // 创建文件保存字典
                this.fileMap = new Dictionary<string, Dictionary<string, string>>();

                // 解析数据
                return FileToData(data);
            }
            catch (System.Exception ex)
            {
                var msg = string.Format("{0}：IniStream.OpenRead({1}) error: {2}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), filePath, ex.Message);
                MachineCtrl.GetInstance().WriteLog(msg, $"{MachineCtrl.GetInstance().ProductionFilePath}\\LogFile", "DataSave.log");
                Trace.WriteLine(msg);
            }
            return false;
        }

        /// <summary>
        /// 清除数据和删除文件
        /// </summary>
        /// <returns></returns>
        public bool DeleteFile()
        {
            try
            {
                this.fileLock.EnterWriteLock();
                this.fileMap.Clear();

                File.Delete(this.fileName);
                return true;
            }
            catch (System.Exception ex)
            {
                var msg = string.Format("IniStream.DeleteFile() error: {0}", ex.Message);
                MachineCtrl.GetInstance().WriteLog(msg, $"{MachineCtrl.GetInstance().ProductionFilePath}\\LogFile", "DataSave.log");
                Trace.WriteLine(msg);
            }
            finally
            {
                this.fileLock.ExitWriteLock();
            }
            return false;
        }

        /// <summary>
        /// 清空数据和文件
        /// </summary>
        public void ClearData()
        {
            try
            {
                this.fileLock.EnterWriteLock();
                this.fileMap.Clear();

                // 清空文件内容
                FileStream fs = new FileStream(this.fileName, FileMode.Truncate, FileAccess.ReadWrite);
                fs.Close();
                fs.Dispose();
            }
            catch (System.Exception ex)
            {
                var msg = string.Format("{0}：IniStreamEx.ClearData() error: {1}", DateTime.Now.ToString(), ex.Message);
                MachineCtrl.GetInstance().WriteLog(msg, $"{MachineCtrl.GetInstance().ProductionFilePath}\\LogFile", "DataSave.log");
                Trace.WriteLine(msg);
            }
            finally
            {
                this.fileLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 解析文件内容
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool FileToData(string buffer)
        {
            try
            {
                Dictionary<string, string> emptySection = new Dictionary<string, string>();
                string[] data = buffer.Split((new char[] { '\r', '\n' }), StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < data.Length; i++)
                {
                    Dictionary<string, string> kv = new Dictionary<string, string>();
                    // 包含节
                    if (data[i].Contains('[') && data[i].Contains(']'))
                    {
                        string section = data[i].Trim('[', ']');
                        i++;    // 下一行起遍历

                        // 遍历节中包含的所有键值对
                        for (; i < data.Length; i++)
                        {
                            if (data[i].Contains("="))
                            {
                                int idx = data[i].IndexOf("=", StringComparison.CurrentCulture);
                                if (idx > 0)
                                {
                                    string key = data[i].Substring(0, idx).Trim();
                                    if (kv.ContainsKey(key))
                                    {
                                        kv[key] = data[i].Substring(idx + 1).Trim();
                                    }
                                    else
                                    {
                                        kv.Add(key, data[i].Substring(idx + 1).Trim());
                                    }
                                }
                            }
                            else if ("" != data[i])
                            {
                                break;
                            }
                        }
                        this.fileLock.EnterWriteLock();
                        if (this.fileMap.ContainsKey(section))
                        {
                            this.fileMap[section] = kv;
                        }
                        else
                        {
                            this.fileMap.Add(section, kv);
                        }
                        this.fileLock.ExitWriteLock();
                        i--;    // 内循环多遍历一次
                    }
                    // 不包含节
                    else if ("" != data[i])
                    {
                        string section = data[i].Trim('[', ']');
                        int idx = data[i].IndexOf("=", StringComparison.CurrentCulture);
                        if (idx > -1)
                        {
                            string key = data[i].Substring(0, idx).Trim();
                            if (emptySection.ContainsKey(key))
                            {
                                emptySection[key] = data[i].Substring(idx + 1).Trim();
                            }
                            else
                            {
                                emptySection.Add(key, data[i].Substring(idx + 1).Trim());
                            }
                        }
                    }
                }
                if (emptySection.Count > 0)
                {
                    this.fileLock.EnterWriteLock();
                    this.fileMap.Add("", emptySection);
                    this.fileLock.ExitWriteLock();
                }
                return true;
            }
            catch (System.Exception ex)
            {
                if (this.fileLock.IsWriteLockHeld)
                {
                    this.fileLock.ExitWriteLock();
                }
                var msg = string.Format("{0}：IniStreamEx.FileToData() error: {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ex.Message);
                Trace.WriteLine(msg);

            }
            return false;
        }

        /// <summary>
        /// 保存数据到文件
        /// </summary>
        /// <returns></returns>
        public bool DataToFile()
        {
            try
            {
                this.fileLock.EnterWriteLock();
                using (StreamWriter sw = new StreamWriter(this.fileName, false))
                {
                    if (this.fileMap.ContainsKey(""))
                    {
                        foreach (var kv in this.fileMap[""])
                        {
                            sw.WriteLine("{0} = {1}", kv.Key, kv.Value);
                        }
                        sw.WriteLine();
                    }
                    foreach (var section in this.fileMap)
                    {
                        if ("" != section.Key)
                        {
                            sw.WriteLine("[{0}]", section.Key);
                            foreach (var kv in section.Value)
                            {
                                sw.WriteLine("{0} = {1}", kv.Key, kv.Value);
                            }
                            sw.WriteLine();
                        }
                    }
                }
                return true;
            }
            catch (System.Exception ex)
            {
                var msg = string.Format("{0}：IniStream.DataToFile() error: {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ex.Message);
                Trace.WriteLine(msg);
            }
            finally
            {
                this.fileLock.ExitWriteLock();
            }
            return false;
        }


        /// <summary>
        /// 写string
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool WriteString(string section, string key, string value)
        {
            try
            {
                this.fileLock.EnterWriteLock();
                if (this.fileMap.ContainsKey(section))
                {
                    if (this.fileMap[section].ContainsKey(key))
                    {
                        this.fileMap[section][key] = value;
                    }
                    else
                    {
                        this.fileMap[section].Add(key, value);
                    }
                }
                else
                {
                    Dictionary<string, string> kv = new Dictionary<string, string>();
                    kv.Add(key, value);
                    this.fileMap.Add(section, kv);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                var msg = string.Format("{0}：IniStreamEx.WriteString() error: {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ex.Message);
                Trace.WriteLine(msg);
            }
            finally
            {
                this.fileLock.ExitWriteLock();
            }
            return false;
        }

        /// <summary>
        /// 写int
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool WriteInt(string section, string key, int value)
        {
            return WriteString(section, key, value.ToString());
        }

        /// <summary>
        /// 写bool
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool WriteBool(string section, string key, bool value)
        {
            return WriteString(section, key, value.ToString());
        }

        /// <summary>
        /// 写double
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool WriteDouble(string section, string key, double value)
        {
            return WriteString(section, key, value.ToString());
        }


        /// <summary>
        /// 读string
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public string ReadString(string section, string key, string defaultValue)
        {
            try
            {
                this.fileLock.EnterReadLock();
                if (this.fileMap.ContainsKey(section))
                {
                    if (this.fileMap[section].ContainsKey(key))
                    {
                        return this.fileMap[section][key];
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(string.Format("IniStream.WriteContent() error: {0}", ex.Message));
            }
            finally
            {
                this.fileLock.ExitReadLock();
            }
            return defaultValue;
        }

        /// <summary>
        /// 读int
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public int ReadInt(string section, string key, int defaultValue)
        {
            try
            {
                return Convert.ToInt32(ReadString(section, key, defaultValue.ToString()));
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(string.Format("IniStream.ReadInt() error: {0}", ex.Message));
            }
            return defaultValue;
        }

        /// <summary>
        /// 读bool
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public bool ReadBool(string section, string key, bool defaultValue)
        {
            try
            {
                return Convert.ToBoolean(ReadString(section, key, defaultValue.ToString()));
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(string.Format("IniStream.ReadBool() error: {0}", ex.Message));
            }
            return defaultValue;
        }

        /// <summary>
        /// 读double
        /// </summary>
        /// <param name="section">节</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public double ReadDouble(string section, string key, double defaultValue)
        {
            try
            {
                return Convert.ToDouble(ReadString(section, key, defaultValue.ToString()));
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(string.Format("IniStream.ReadDouble() error: {0}", ex.Message));
            }
            return defaultValue;
        }

        #endregion
    }
}
