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
using System.Windows.Shapes;
using UpgradeUpdateRvt.ViewModel;

namespace UpgradeUpdateRvt
{
    /// <summary>
    /// Logique d'interaction pour ApplicatioUI.xaml
    /// </summary>
    public partial class ApplicatioUI : Window
    {
        public ApplicatioUI()
        {
            InitializeComponent();
            DataContext = new MainVM();
        }
    }
}
