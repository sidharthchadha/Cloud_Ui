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
    /// Interaction logic for InsightPage4.xaml
    /// </summary>
    public sealed partial class InsightPage4 : Page
    {
        private readonly InsightsApi _insightsApi;

        public InsightPage4()
        {
            this.InitializeComponent();

            // Initialize InsightsApi with the appropriate insightsRoute
            _insightsApi = new InsightsApi("your_insights_route_here");
        }

        private async void OnGetStudentAverageClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Replace "hostname" and "studentName" with actual values
                string hostname = "your_hostname_here";
                string studentName = "your_studentName_here";

                // Call the RunningAverageOnGivenStudent method from InsightsApi
                var averageList = await _insightsApi.RunningAverageOnGivenStudent(hostname, studentName);

                // Display the result in the TextBlock
                ResultTextBlock.Text = "Student Average: " + string.Join(", ", averageList);
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately (e.g., display an error message)
                ResultTextBlock.Text = "Error: " + ex.Message;
            }
        }
    }

}
