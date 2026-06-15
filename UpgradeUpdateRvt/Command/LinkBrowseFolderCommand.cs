using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UpgradeUpdateRvt.ViewModel;

namespace UpgradeUpdateRvt.Command
{
    internal class LinkBrowseFolderCommand : ICommand
    {
        private LinkRvtVM _linkRvtVM;
        public LinkBrowseFolderCommand(LinkRvtVM linkRvtVM) { _linkRvtVM = linkRvtVM; }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
           return true;
        }

        public void Execute(object parameter)
        {
            // 1. Initialisation du dialogue moderne
            OpenFolderDialog dialogue = new OpenFolderDialog
            {
                Title = "Sélectionner un dossier de travail", // Titre de la fenêtre
               // InitialDirectory = @"C:\",                   // Dossier d'ouverture par défaut
                Multiselect = false                           // Définir sur 'true' pour autoriser plusieurs dossiers
            };

            // 2. Affichage du dialogue (ShowDialog retourne un booléen nullable)
            bool? resultat = dialogue.ShowDialog();

            // 3. Traitement du résultat
            if (resultat == true)
            {
                // Retourne le chemin complet du dossier sélectionné
                _linkRvtVM.LinkModel.MainDirectory = dialogue.FolderName;
                if (_linkRvtVM.LinkModel.IsIfcFiles)
                {
                    _linkRvtVM.LinkModel.LoadIfcFiles();

                }
                else
                {
                    _linkRvtVM.LinkModel.LoadFiles();
                }

            }

            

        }
    }
}
