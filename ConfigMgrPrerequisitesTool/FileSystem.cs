using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;

namespace ConfigMgrPrerequisitesTool
{
    class FileSystem : INotifyPropertyChanged
    {
        public string VolumeLabel { get; set; }
        public string DriveName { get; set; }
        public string DriveFreeSpace { get; set; }
        private bool _DriveSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///  This method triggers the PropertyChanged event and is used when properties
        ///  in a data grid has been programmatically changed.
        /// </summary>
        public void OnPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool DriveSelected
        {
            get { return _DriveSelected; }
            set
            {
                if (_DriveSelected != value)
                {
                    _DriveSelected = value;
                    OnPropertyChanged("DriveSelected");
                }
            }
        }

        private string ConvertFromBytes(double bytes)
        {
            string[] suffix = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int index = 0;
            do {
                bytes /= 1024; index++;
            }
            while (bytes >= 1024);

            return String.Format("{0:0.00} {1}", bytes, suffix[index]);
        }

        public bool IsFolderEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        public string GetParentFolder(string path)
        {
            return System.IO.Directory.GetParent(path).FullName;
        }

        public List<FileSystem> GetVolumeInfo()
        {
            //' Construct new list for all volumes
            List<FileSystem> volumeInfo = new List<FileSystem>();

            //' Get all volumes
            DriveInfo[] volumes = DriveInfo.GetDrives();

            foreach (DriveInfo volume in volumes)
            {
                if (volume.IsReady && volume.DriveType == DriveType.Fixed && volume.DriveFormat == "NTFS")
                {
                    volumeInfo.Add(new FileSystem { DriveSelected = false, VolumeLabel = volume.VolumeLabel, DriveName = volume.Name, DriveFreeSpace = ConvertFromBytes(volume.AvailableFreeSpace) });
                }
            }

            return volumeInfo;
        }

        async public Task WriteFileAsync(string path, string text)
        {
            string fileName = @"NO_SMS_ON_DRIVE.SMS";
            string filePath = Path.Combine(path, fileName);

            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        async public Task CopyFileAsync(string sourceFile, string destinationFile, CancellationToken cancellationToken)
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 4096;

            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))
            using (var destinationStream = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, fileOptions))

            await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}