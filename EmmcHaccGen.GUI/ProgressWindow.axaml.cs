using Avalonia.Controls;
using Avalonia.Threading;

namespace EmmcHaccGen.GUI
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }
        
        public void UpdateProgress(int percentage, string statusText)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ProgressBar.Value = percentage;
                PercentText.Text = $"{percentage}%";
                StatusText.Text = statusText;
            });
        }
    }
}
