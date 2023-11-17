using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ServerlessFunc;

namespace ServerlessFuncUI
{
    /// <summary>
    /// Interaction logic for InsightPage5.xaml
    /// </summary>
    public partial class InsightPage5 : Page
    {
        private readonly InsightsApi _insightsApi;

        public InsightPage5()
        {
            InitializeComponent();
            _insightsApi = new InsightsApi("your_insights_route"); // Replace with your actual insights route
        }

        private async void OnGetRunningAverageClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string hostname = "your_hostname"; // Replace with your actual hostname
                List<double> averageList = await _insightsApi.RunningAverageAcrossSessoins(hostname);

                // Clear existing items and add new average values to the ListView
                AverageListView.Items.Clear();
                foreach (var average in averageList)
                {
                    AverageListView.Items.Add(average);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
