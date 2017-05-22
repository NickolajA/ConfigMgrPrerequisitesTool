using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigMgrPrerequisitesTool
{
    public class WindowsFeature : INotifyPropertyChanged
    {
        public string Server { get; set; }
        public string Name { get; set; }
        private bool _Progress;
        private string _Result;

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

        public bool Progress
        {
            get { return _Progress; }
            set
            {
                if (_Progress != value)
                {
                    _Progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }

        public string Result
        {
            get { return _Result; }
            set
            {
                if (_Result != value)
                {
                    _Result = value;
                    OnPropertyChanged("Result");
                }
            }
        }
    }
}
