/*
 * Simple Conway's Game of Life implementation in C#.
 * Copyright (C) 2014   Lukas Fehmer
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Windows;

namespace GameOfLife {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private static MainWindow myInstance;
        private ConwaysGame game;

        public MainWindow() {
            InitializeComponent();
            game = new ConwaysGame(GameCanvas);
            game.DrawGrid();
            Instance = this;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e) {
            game.Clear();
            game.Refresh();
        }

        private void btnRandom_Click(object sender, RoutedEventArgs e) {
            game.SetRandomStart();
            game.Refresh();
            btnStart.IsEnabled = true;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e) {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnRandom.IsEnabled = false;
            btnClear.IsEnabled = false;
            sldNCell.IsEnabled = false;
            game.Start();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e) {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnRandom.IsEnabled = true;
            btnClear.IsEnabled = true;
            sldNCell.IsEnabled = true;
            game.Stop();
        }

        private void sldDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if ((sldDelay == null) || (game == null))
                return;
            int value = (int)sldDelay.Value;
            lblDelay.Content = value.ToString();
            game.MsDelay = value;
        }

        private void sldNCell_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if ((sldNCell == null) || (game == null))
                return;
            int value = (int)sldNCell.Value;
            lblNCell.Content = value.ToString();
            game.NCells = value;
            game.DrawGrid();
        }

        public static MainWindow Instance {
            get { return MainWindow.myInstance; }
            private set { MainWindow.myInstance = value; }
        }
    }
}