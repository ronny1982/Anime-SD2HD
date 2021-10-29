using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AnimeSD2HD
{
    internal abstract class ViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> properties = new();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(params string[] properties)
        {
            foreach(var property in properties)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        protected T GetPropertyValue<T>([CallerMemberName] string property = "")
        {
            return properties.TryGetValue(property, out var value) ? (T)value : default;
        }

        protected void SetPropertyValue<T>(T value, [CallerMemberName] string property = "")
        {
            if (!properties.TryGetValue(property, out var result) || !result.Equals(value))
            {
                properties[property] = value;
                RaisePropertyChanged(property);
            }
        }
    }
}