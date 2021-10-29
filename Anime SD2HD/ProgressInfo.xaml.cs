using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AnimeSD2HD
{
    internal sealed partial class ProgressInfo : UserControl
    {
        public ProgressInfo()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(ProgressInfoViewModel), typeof(ProgressInfo), null);
        public ProgressInfoViewModel ViewModel
        {
            get => (ProgressInfoViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
    }
}