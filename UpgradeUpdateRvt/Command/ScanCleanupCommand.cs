using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using UpgradeUpdateRvt.ViewModel;

namespace UpgradeUpdateRvt.Command
{
    internal class ScanCleanupCommand : ICommand
    {
        private MainVM _mainVM;
        public ScanCleanupCommand(MainVM mainVM) { _mainVM = mainVM; }

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var init = _mainVM.Init;
            if (string.IsNullOrEmpty(init.DirectoryPath) || !Directory.Exists(init.DirectoryPath))
            {
                MessageBox.Show("Veuillez d'abord sélectionner un dossier valide.", "Dossier manquant", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(init.CleanupPatterns))
            {
                MessageBox.Show("Veuillez spécifier au moins un motif de nettoyage (ex: Revit_temp, *R26*).", "Motifs manquants", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                init.AnalyserDossierPourNettoyage();
                if (init.CleanupList.Count == 0)
                {
                    MessageBox.Show("Aucun dossier ou fichier ne correspond aux motifs saisis dans le répertoire sélectionné.", "Aucun élément trouvé", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur est survenue lors de l'analyse : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
