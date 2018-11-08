﻿using System;
using System.IO;

namespace XSLibrary.Utility
{
    public class FileLogger : Logger
    {
        string FilePath { get; set; }

        public FileLogger(string filePath)
        {
            FilePath = filePath;
            CheckFile();
        }

        protected override void LogMessage(string text)
        {
            string withDate = string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), text);
            File.AppendAllLines(FilePath, new string[1] { withDate });
        }

        private void CheckFile()
        {
            if(!Directory.Exists(Path.GetDirectoryName(FilePath)))
            {
                Directory.CreateDirectory(FilePath);
            }

            if(!File.Exists(FilePath))
            {
                File.Create(FilePath).Close();
            }
        }
    }
}
