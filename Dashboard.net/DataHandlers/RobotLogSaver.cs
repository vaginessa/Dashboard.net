﻿using System;
using System.ComponentModel;
using System.IO;

namespace Dashboard.net.DataHandlers
{
    public static class RobotLogSaver
    {

        public enum TypeOfSave
        {
            OverwriteOrCreate = 1, AppendOrCreate
        }

        // We're saving these as log files. Folder path is the folder location for the logs.
        private static readonly string EXTENSION = ".log";

        /// <summary>
        /// The full directory path for the logs
        /// </summary>
        private static string DirectoryPath
        {
            get
            {
                return Path.Combine(DataDealer.DataLocation, "RobotLogs");
            }
        }

        /// <summary>
        /// Saves the given log data to the file name given
        /// </summary>
        /// <param name="dataToSave"></param>
        /// <param name="fileName">The file name where we will be saving the file. Should not include the extension,
        /// but if it does it gets rid of it.</param>
        /// <param name="typeOfSave">The type of saving to be doing, either overwriting the file, appending
        /// to it or appending and creating a new file if it doesn't exist.</param>
        public static void SaveLogData(string dataToSave, string fileName, TypeOfSave typeOfSave = TypeOfSave.AppendOrCreate)
        {
            // Seperate the data into a string array based on the newline characters
            string[] logDataToWrite = dataToSave.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None
                );


            // Format the file name properly
            fileName = FormatFileName(fileName);

            // If the directory doesn't exist, create it.
            if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);

            // Add the directory to the file name.
            fileName = Path.Combine(DirectoryPath, fileName);


            FileStream logFile;
            // If the file doesn't exist, create it or overwrite it if that's the selected option.
            if (!File.Exists(fileName) || typeOfSave == TypeOfSave.OverwriteOrCreate) logFile = File.Create(fileName);
            else logFile = new FileStream(fileName, FileMode.Append);

            // Use stream writer to write the data.
            using (StreamWriter writer = new StreamWriter(logFile))
            {
                writer.WriteLine(dataToSave);
            }
        }

        /// <summary>
        /// Formats the file name so that it has the proper extension and name
        /// </summary>
        /// <param name="fileName">The file name to format</param>
        /// <returns>The properly formatted file name (with right extension)</returns>
        private static string FormatFileName(string fileName)
        {
            // Get rid of any extension on the file name.
            if (fileName.Contains("."))
            {
                fileName = fileName.Substring(0, fileName.IndexOf("."));
            }

            // Get rid of spaces
            fileName = fileName.Trim();
            
            // Add the extension to the file name
            fileName += EXTENSION;

            return fileName;

        }
    }
}
