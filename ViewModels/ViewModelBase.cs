using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace PDFPages.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region INotifyDataErrorInfo

        [JsonIgnore]
        public bool HasErrors { get; set; } = false;
        [JsonIgnore]
        public ObservableDictionary<string, string> ErrorList { get; set; } = new ObservableDictionary<string, string>();

        [JsonIgnore]
        public string GetAllErrors
        {
            get
            {
                if (!HasErrors) return null;
                int count = 1;
                var errors = "Errors:";
                foreach (var e in ErrorList)
                {
                    errors += "\n" + count++ + ". " + e.Value;
                }
                return errors;
            }
        }


        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        public void NotifyErrorsChanged(string PropertyName) { ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(PropertyName)); }

        public IEnumerable GetErrors(string propertyName)
        {
            if (!HasErrors || propertyName == null) return null;
            if (ErrorList.Keys.Contains(propertyName)) return new List<string>() { ErrorList[propertyName] };
            if (propertyName != "GetAllErrors") NotifyPropertyChanged("GetAllErrors");
            return new List<string>();
        }
        /// <summary>
        /// Adds an error to the selected property.
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="ErrorMessage"></param>
        /// <param name="Notify">Set to false to simply change the error state, without updating the display.</param>
        public void AddError(string PropertyName, string ErrorMessage, bool Notify = true)
        {
            if (!ErrorList.ContainsKey(PropertyName))
            {
                ErrorList.Add(PropertyName, ErrorMessage);
                HasErrors = true;
                if (Notify) NotifyErrorsChanged(PropertyName);
            }
        }
        /// <summary>
        /// Display an error without out maintaining the error state of the property. Any changes to the property will reset the error visual.
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="ErrorMessage"></param>
        public void ShowError(string PropertyName, string ErrorMessage)
        {
            AddError(PropertyName, ErrorMessage);
            RemoveError(PropertyName, false);
        }
        /// <summary>
        /// Display an error, with a timer to hide the error after the specified timeout period.
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="ErrorMessage"></param>
        /// <param name="TimeoutMS">Number of milliseconds before the error is removed.</param>
        public void ShowError(string PropertyName, string ErrorMessage, int TimeoutMS)
        {
            AddError(PropertyName, ErrorMessage);
            Task.Delay(TimeoutMS).ContinueWith(_ => App.Current.Dispatcher.Invoke(() => RemoveError(PropertyName)));
            //RemoveError(PropertyName, false);
        }


        public void RemoveError(string PropertyName, bool Notify = true)
        {
            if (ErrorList.ContainsKey(PropertyName))
            {
                ErrorList.Remove(PropertyName);
                HasErrors = ErrorList.Count > 0;
            }
            if (Notify) ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(PropertyName));
        }
        public void ClearErrors()
        {
            var removalList = new Dictionary<string, string>(ErrorList);
            ErrorList.Clear();
            HasErrors = false;
            foreach (var propertyName in removalList.Keys) ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        #endregion INotifyDataErrorInfo

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); }

        #endregion INotifyPropertyChanged
    }
}
