using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Net;

namespace WebFileApi.FileApi
{
    [Route("fileapi/[controller]/{action}")]
    [ApiController]
    public class FileController : ControllerBase
    {
        /// <summary>
        /// 测试方法
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "Test")]
        public string Test()
        {
            return "test";
        }

        /// <summary>
        /// 保存文件到对应路径
        /// </summary>
        /// <param name="path">路径，精确到文件名称</param>
        /// <returns>成功返回true,不存在文件则返回false，报错返回报错信息</returns>
        [HttpPost(Name = "SaveFileToPath")]
        public string SaveFileToPath(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                string result = ChankPath(fileInfo.Directory?.FullName ?? string.Empty);

                if (!result.Equals("true"))
                {
                    return "创建文件夹失败:" + result;
                }

                Request.EnableBuffering();
                Request.Body.Position = 0;

                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    int bufferSize = 409600;
                    Task<int> readCount;
                    byte[] buffer = new byte[bufferSize];
                    //readCount = stream.Read(buffer, 0, bufferSize);
                    //.netcode不支持 同步， 需要修改允许同步的属性
                    readCount = Request.Body.ReadAsync(buffer, 0, bufferSize);

                    while (readCount.Result > 0)
                    {
                        fs.Write(buffer, 0, readCount.Result);
                        readCount = Request.Body.ReadAsync(buffer, 0, bufferSize);
                    }
                }

                return fileInfo.Exists.ToString().ToLower();
            }
            catch (Exception ex)
            {
                return "保存文件失败:" + ex.ToString();
            }
        }

        /// <summary>
        /// 检查文件夹路径是否存在，不存在则创建
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns>成功返回true,不存在文件则返回false，报错返回报错信息</returns>
        [HttpPost(Name = "ChankPath")]
        public string ChankPath(string path)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);

                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }

                return directoryInfo.Exists.ToString().ToLower();
            }
            catch (Exception ex)
            {
                return "发生错误:" + ex.ToString();
            }
        }

        /// <summary>
        /// 移动文件到另一个路径下
        /// </summary>
        /// <param name="path">需要移动的文件</param>
        /// <param name="toPath">目标文件</param>
        /// <returns>成功返回true,不存在文件则返回false，报错返回报错信息</returns>
        [HttpPost(Name = "MoveFile")]
        public string MoveFile(string path, string toPath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                FileInfo toFileInfo = new FileInfo(toPath);
                DirectoryInfo directoryInfo = new DirectoryInfo(toFileInfo.Directory?.FullName??String.Empty);

                if (!fileInfo.Exists)
                {
                    return "'path'对应路径不存在!";
                }

                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }

                if (fileInfo.Name.Equals("check.txt"))
                {
                    return "true";
                }

                fileInfo.MoveTo(toPath, true);

                return toFileInfo.Exists.ToString().ToLower();
            }
            catch (Exception ex)
            {
                return "发生错误:" + ex.ToString();
            }
        }

        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="path">需要删除的文件夹</param>
        /// <returns>成功返回true,不存在文件则返回false，报错返回报错信息</returns>
        [HttpPost(Name = "DeleteDirectory")]
        public string DeleteDirectory(string path)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);

                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(true);
                }

                return "true";
            }
            catch (Exception ex)
            {
                return "发生错误:" + ex.ToString();
            }
        }

        /// <summary>
        /// 移动文件夹
        /// </summary>
        /// <param name="path">需要移动的文件夹</param>
        /// <param name="toPath">目标文件夹</param>
        /// <returns>成功返回true,不存在文件则返回false，报错返回报错信息</returns>
        [HttpPost(Name = "MoveDirectory")]
        public string MoveDirectory(string path, string toPath)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);

                if (!directoryInfo.Exists)
                {
                    return "'path'对应路径不存在!";
                }

                FileInfo[] fileInfops = directoryInfo.GetFiles();

                if (fileInfops.Length < 1)
                {
                    return "'path'下无可移动文件!";
                }

                for (int i = 0; i < fileInfops.Length; i++)
                {
                    if(!MoveFile(fileInfops[i].FullName, Path.Combine(toPath, fileInfops[i].Name)).Equals("true"))
                    {
                        return "移动文件失败!";
                    }
                }

                return directoryInfo.Exists.ToString().ToLower();
            }
            catch (Exception ex)
            {
                return "发生错误:" + ex.ToString();
            }
        }

        /// <summary>
        /// 得到路径下文件数量
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns></returns>
        [HttpPost(Name = "GetFileCount")]
        public string GetFileCount(string path)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);

                return directoryInfo.GetFiles().Length.ToString().ToLower();
            }
            catch (Exception ex)
            {
                return "发生错误:" + ex.ToString();
            }
        }

        /// <summary>
        /// 获取指定文件的已上传的文件块
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "GetMaxChunk")]
        public string GetMaxChunk(string root, string md5, string ext)
        {
            try
            {
                int chunk = 0;

                string fileName = md5 + "." + ext;

                FileInfo file = new FileInfo(root + fileName);

                if (file.Exists)
                {
                    chunk = Int32.MaxValue;
                }
                else
                {
                    if (Directory.Exists(root + "chunk\\" + md5))
                    {
                        DirectoryInfo dicInfo = new DirectoryInfo(root + "chunk\\" + md5);
                        chunk = dicInfo.GetFiles().Length;

                        if (chunk > 0)
                        {
                            //当文件上传中时，页面刷新，上传中断，这时最后一个保存的块的大小可能会有异常，所以这里直接删除最后一个块文件
                            chunk = chunk - 1;

                            if (chunk > 0)
                            {
                                FileInfo[] fileInfos = dicInfo.GetFiles();

                                for (int i = 0; i < fileInfos.Length; i++)
                                {
                                    if (!fileInfos[i].Name.Equals(i.ToString()))
                                    {
                                        DeleteDirectory(fileInfos[i].Directory?.FullName??string.Empty);
                                        chunk = 0;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                return chunk.ToString();
            }
            catch(Exception ex)
            {
                return "出错了:" + ex.ToString();
            }
        }

        /// <summary>
        /// 合并文件
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "MergeFiles")]
        public string MergeFiles(string root, string guid, string ext)
        {
            string sourcePath = Path.Combine(root, "chunk\\" + guid + "\\");//源数据文件夹
            string targetPath = Path.Combine(root, guid + ext);//合并后的文件

            DirectoryInfo dicInfo = new DirectoryInfo(sourcePath);
            if (dicInfo.Exists)
            {
                FileInfo[] files = dicInfo.GetFiles();

                foreach (FileInfo file in files.OrderBy(f => int.Parse(f.Name)))
                {
                    using (FileStream addFile = new FileStream(targetPath, FileMode.Append, FileAccess.Write))
                    {
                        using (BinaryWriter AddWriter = new BinaryWriter(addFile))
                        {
                            //获得上传的分片数据流 
                            using (Stream stream = file.Open(FileMode.Open))
                            {
                                using (BinaryReader TempReader = new BinaryReader(stream))
                                {
                                    //将上传的分片追加到临时文件末尾
                                    AddWriter.Write(TempReader.ReadBytes((int)stream.Length));
                                }
                            }
                        }
                    }
                }

                return DeleteDirectory(sourcePath);
            }
            else
            {
                return "未找到源文件:" + sourcePath;
            }
        }

        /// <summary>
        /// 解压RAR文件夹
        /// </summary>
        /// <param name="path">需要解压的文件</param>
        /// <param name="toPath">目标文件夹</param>
        /// <param name="password">文件密码</param>
        /// <returns>成功返回true,不存在文件则返回false，报错返回报错信息</returns>
        [HttpGet(Name = "UnZipRaR")]
        public string UnZipRaR(string path, string toPath, string password)
        {
            try
            {
                DirectoryInfo dicInfo = new DirectoryInfo(toPath);

                if (!dicInfo.Exists)
                {
                    dicInfo.Create();
                }

                return ExcuteWinRar(" x -p" + password + " -y " + path + " " + toPath).ToString().ToLower();
            }
            catch (Exception ex)
            {
                return "发生错误:" + ex.ToString();
            }
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="excute"></param>
        /// <returns></returns>
        bool ExcuteWinRar(string excute)
        {
            System.Diagnostics.Process Process1 = new System.Diagnostics.Process();

            try
            {
                Process1.StartInfo.FileName = "C:\\Program Files\\WinRAR\\WinRAR.exe";
                Process1.StartInfo.CreateNoWindow = true;
                Process1.StartInfo.Arguments = excute;
                Process1.Start();
                //30分钟
                Process1.WaitForExit(1800000);

                return (Process1.ExitCode == 0 || Process1.ExitCode == 1) ? Process1?.HasExited ?? false : false;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                Process1?.Kill(true);
            }
        }

        /// <summary>
        /// 读取文本内容
        /// </summary>
        /// <param name="path">读取的路径</param>
        /// <returns>成功返回内容,不存在文件则返回false，报错返回报错信息</returns>
        [HttpPost(Name = "ReadTxtContent")]
        public string ReadTxtContent(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);

                if (!fileInfo.Extension.ToLower().Equals(".txt") || !fileInfo.Exists)
                {
                    return "false";
                }

                using (StreamReader sr = fileInfo.OpenText())
                {
                    string line;

                    // 从文件读取并显示行，直到文件的末尾 
                    while ((line = sr.ReadLine()?.Trim() ?? String.Empty) != null)
                    {
                        return line;
                    }

                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                return "发生错误:" + ex.ToString();
            }
        }

        /// <summary>
        /// 检查gdb文件合法性
        /// </summary>
        /// <param name="root">根目录</param>
        /// <param name="guid">名称</param>
        /// <param name="ext">后缀</param>
        ///<param name="filename">文件解压后名称</param>
        /// <param name="password">密码</param>
        /// <param name="chanck">是否需要检查文件 1检查 2不检查</param>
        /// <returns>成功返回true,不存在文件则返回false，报错返回报错信息</returns>
        [HttpPost(Name = "ChanckGDB")]
        public string ChanckGDB(string root, string guid, string ext, string filename, string password, string chanck)
        {
            try
            {
                string targetFilePath = Path.Combine(root, guid + ext);
                FileInfo fileInfo = new FileInfo(targetFilePath);
                string chanckFile = fileInfo?.DirectoryName ?? string.Empty;

                if (!UnZipRaR(targetFilePath, chanckFile, password).Equals("true"))
                {
                    return "false";
                }

                if (chanck.Equals("2"))
                {
                    return "true";
                }

                string txt = ReadTxtContent(chanckFile + "\\" + filename+ "\\check.txt");
                string[] tempReult = txt.Split(',');

                return (tempReult.Length == 2 && tempReult[0].Equals("true")).ToString().ToLower();
            }
            catch (Exception ex)
            {
                return "发生错误:" + ex.ToString();
            }
        }
    }
}
