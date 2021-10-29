using Microsoft.UI.Xaml;
using System;

namespace AnimeSD2HD
{
    internal class ProgressInfoViewModel : ViewModel
    {
        public ProgressInfoViewModel(bool show, bool indeterminate, double min, double max, double value, TimeSpan elapsed)
        {
            VisibleState = show ? Visibility.Visible : Visibility.Collapsed;
            IsIndeterminate = indeterminate;
            Minimum = min;
            Maximum = max;
            Value = value;
            Elapsed = elapsed.ToString(@"hh\:mm\:ss");
            // TODO: handle minimum != 0 ...
            Remaining = (IsIndeterminate || Value == 0d ? -TimeSpan.Zero : elapsed * (Maximum - Value) / Value).ToString(@"hh\:mm\:ss");
            Duration = (IsIndeterminate || Value == 0d ? -TimeSpan.Zero : elapsed * Maximum / Value).ToString(@"hh\:mm\:ss");
        }

        public Visibility VisibleState {
            get => GetPropertyValue<Visibility>();
            private set => SetPropertyValue(value);
        }
        public bool IsIndeterminate
        {
            get => GetPropertyValue<bool>();
            private set => SetPropertyValue(value);
        }
        public double Minimum
        {
            get => GetPropertyValue<double>();
            private set => SetPropertyValue(value);
        }
        public double Maximum
        {
            get => GetPropertyValue<double>();
            private set => SetPropertyValue(value);
        }
        public double Value
        {
            get => GetPropertyValue<double>();
            private set => SetPropertyValue(value);
        }
        public string Elapsed
        {
            get => GetPropertyValue<string>();
            private set => SetPropertyValue(value);
        }
        public string Remaining
        {
            get => GetPropertyValue<string>();
            private set => SetPropertyValue(value);
        }
        public string Duration
        {
            get => GetPropertyValue<string>();
            private set => SetPropertyValue(value);
        }
    }
}