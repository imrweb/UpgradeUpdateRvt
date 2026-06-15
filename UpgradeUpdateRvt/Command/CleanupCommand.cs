using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using UpgradeUpdateRvt.ViewModel;

namespace UpgradeUpdateRvt.Command
{
    internal class CleanupCommand : ICommand
    {
        private MainVM _mainVM;
        public CleanupCommand(MainVM mainVM) { _mainVM = mainVM; }

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
            
            // Count selected items
            int itemsToDeleteCount = 0;
            foreach (var item in init.CleanupList)
            {
                if (item.IsSelected)
                {
                    itemsToDeleteCount++;
                }
            }

            if (itemsToDeleteCount == 0)
            {
                MessageBox.Show("Aucun élément n'est sélectionné pour la suppression. Veuillez cocher les éléments à supprimer dans le tableau, ou cliquez sur 'Analyser le dossier' pour actualiser la liste.", "Aucun élément sélectionné", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string message = $"Voulez-vous vraiment supprimer définitivement les {itemsToDeleteCount} dossiers et/ou fichiers cochés dans la liste ?";
            var result = MessageBox.Show(message, "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    init.ExecuterNettoyageSelectionne();
                    MessageBox.Show("Nettoyage effectué avec succès !", "Nettoyage terminé", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Une erreur est survenue lors du nettoyage : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
