using GokApp_PRA.Data;
using GokApp_PRA.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GokApp_PRA
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly ApiManager apiManager;
        private int balance = 1000; // Startbalans van de gebruiker
        private bool hasPlacedBet = false; // Variabele om te controleren of er al een weddenschap is geplaatst

        public MainWindow()
        {
            this.InitializeComponent();
            apiManager = new ApiManager();
            UpdateBalanceText();
        }

        private void UpdateBalanceText()
        {
            balanceText.Text = $"Huidige Balans: €{balance}";
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ContentSection.Visibility = Visibility.Visible;
            StartContent.Visibility = Visibility.Collapsed;

            try
            {
                List<ApiManager.Match> matchesList = await apiManager.GetMatchAsync();

                foreach (var match in matchesList)
                {
                    if (int.TryParse(match.team_1_score, out int _) || int.TryParse(match.team_2_score, out int _))
                    {
                        string content = $"{match.team_1} vs {match.team_2}";
                        matches.Items.Add(new ComboBoxItem { Content = content });
                    }
                }


                if (matches.Items.Count == 0)
                {
                    winnings.Text = "Geen wedstrijden beschikbaar om te selecteren.";
                }

            }
            catch (Exception ex)
            {
                winnings.Text = $"Error: {ex.Message}";
            }
        }

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            if (hasPlacedBet)
            {
                error.Text = "Je hebt al een weddenschap geplaatst!";
                return;
            }

            try
            {
                var selectedTeam = matches.SelectedItem as ComboBoxItem;
                var selectedBet = bet.SelectedItem as ComboBoxItem;
                var amountToBet = amount.Text;
                error.Text = "";

                if (selectedTeam == null)
                {
                    error.Text += "Kies een wedstrijd!\n";
                    return;
                }

                if (selectedBet == null)
                {
                    error.Text += "Kies een uitslag om te gokken\n";
                    return;
                }

                if (!int.TryParse(amountToBet, out int bedrag))
                {
                    error.Text += "Voer een geldig getal in\n";
                    return;
                }

                if (bedrag <= 0)
                {
                    error.Text += "Voer een geldig bedrag in.\n";
                    return;
                }

                if (bedrag > balance)
                {
                    error.Text += "Je hebt niet genoeg balans om te gokken.\n";
                    return;
                }

                balance -= bedrag; // Verminder de balans na het plaatsen van een weddenschap
                UpdateBalanceText(); // Update de balans weergave

                List<ApiManager.Match> matchesList = await apiManager.GetMatchAsync();

                foreach (var match in matchesList)
                {
                    string matchString = $"{match.team_1} vs {match.team_2}";

                    if (!int.TryParse(match.team_1_score, out int team1Score) || !int.TryParse(match.team_2_score, out int team2Score))
                    {
                        continue;
                    }

                    if (selectedTeam.Content.ToString() == matchString)
                    {
                        if (team1Score > team2Score && selectedBet.Content.ToString() == "Team 1" ||
                            team1Score < team2Score && selectedBet.Content.ToString() == "Team 2" ||
                            team1Score == team2Score && selectedBet.Content.ToString() == "Gelijk")
                        {
                            winnings.Text = "Gefeliciteerd, je hebt gewonnen!";
                            balance += selectedBet.Content.ToString() == "Gelijk" ? bedrag : bedrag * 2;
                        }
                        else
                        {
                            winnings.Text = "Helaas, je hebt verloren.";
                        }
                        UpdateBalanceText();
                        hasPlacedBet = true; // Markeer dat een weddenschap is geplaatst
                        return;
                    }
                }

                winnings.Text = "Wedstrijd niet gevonden of scores niet ingevuld!";
            }
            catch (Exception ex)
            {
                winnings.Text = $"Error: {ex.Message}";
            }
        }
    }
}
