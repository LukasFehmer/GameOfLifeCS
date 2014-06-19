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

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GameOfLife {

    public class ConwaysGame {
        private bool[,] BoolField;
        private Brush myBackColor;
        private Brush myBorderColor;
        private byte myBorderSize;
        private CancellationTokenSource myCancellation;
        private Brush myCellColor;
        private Canvas myGameCanvas;
        private Task myGameTask;
        private bool myIsGameRunning;
        private int myMsDelay;
        private int myNCells;
        private Rectangle[,] RectField;

        public ConwaysGame(Canvas pGameCanvas) {
            GameCanvas = pGameCanvas;
            BackColor = Brushes.LightGray;
            BorderColor = Brushes.DarkGray;
            BorderSize = 2;
            CellColor = Brushes.Blue;
            NCells = 10;
            MsDelay = 500;
        }

        public static bool[,] CopyBoolArray(bool[,] arr) {
            bool[,] newArr = new bool[arr.GetLength(0), arr.GetLength(1)];

            for (int row = 0; row < arr.GetLength(0); ++row) {
                for (int col = 0; col < arr.GetLength(1); ++col) {
                    if (arr[row, col])
                        newArr[row, col] = true;
                }
            }

            return newArr;
        }

        public void Clear() {
            BoolField = new bool[NCells, NCells];
        }

        public void DrawGrid() {
            RectField = new Rectangle[NCells, NCells];
            BoolField = new bool[NCells, NCells];
            double cellWidth = (GameCanvas.Width - BorderSize - BorderSize * NCells) / NCells;
            double cellHeight = (GameCanvas.Height - BorderSize - BorderSize * NCells) / NCells;
            Rectangle rect = new Rectangle();
            rect.Width = GameCanvas.Width;
            rect.Height = GameCanvas.Height;
            rect.Fill = BorderColor;
            GameCanvas.Children.Add(rect);

            for (int row = 0; row < NCells; ++row) {
                for (int col = 0; col < NCells; ++col) {
                    rect = new Rectangle();
                    rect.Width = cellWidth;
                    rect.Height = cellHeight;
                    rect.Fill = BackColor;
                    rect.MouseEnter += rect_MouseEnter;
                    rect.MouseLeave += rect_MouseLeave;
                    rect.MouseLeftButtonDown += rect_MouseLeftButtonDown;

                    RectField[row, col] = rect;
                    GameCanvas.Children.Add(rect);
                    Canvas.SetLeft(rect, BorderSize * (col + 1) + cellWidth * col);
                    Canvas.SetTop(rect, BorderSize * (row + 1) + cellHeight * row);
                }
            }
        }

        public void Refresh() {
            for (int row = 0; row < BoolField.GetLength(0); ++row) {
                for (int col = 0; col < BoolField.GetLength(0); ++col) {
                    if (BoolField[row, col]) {
                        RectField[row, col].Fill = CellColor;
                    } else {
                        RectField[row, col].Fill = BackColor;
                    }
                }
            }
        }

        public void SetRandomStart() {
            Clear();
            uint c = 0;
            Random rnd = new Random();
            int row;
            int col;
            int randomSeed = (int)(NCells * NCells * rnd.NextDouble());

            while (c < randomSeed) {
                row = rnd.Next((int)NCells);
                col = rnd.Next((int)NCells);

                if (BoolField[row, col]) continue;

                BoolField[row, col] = true;
                ++c;
            }
        }

        public void Start() {
            myCancellation = new CancellationTokenSource();
            CancellationToken ct = myCancellation.Token;
            myGameTask = Task.Factory.StartNew(
                () => {
                    myIsGameRunning = true;
                    bool[,] newField;
                    int neighbors;

                    while (!ct.IsCancellationRequested) {
                        newField = CopyBoolArray(BoolField);

                        for (int row = 0; row < NCells; ++row) {
                            for (int col = 0; col < NCells; ++col) {
                                neighbors = CountNeighbors(row, col);

                                //- dead cell with three neighbors comes to life again
                                if (!BoolField[row, col] && (neighbors == 3))
                                    newField[row, col] = true;

                                //- life cell with one or less neighbors dies
                                else if (BoolField[row, col] && (neighbors <= 1))
                                    newField[row, col] = false;

                                //- life cell with more than three neighbors dies
                                else if (BoolField[row, col] && (neighbors > 3))
                                    newField[row, col] = false;
                            }
                        }

                        BoolField = newField;
                        MainWindow.Instance.Dispatcher.BeginInvoke(new Action(delegate() {
                            Refresh();
                        })).Wait();
                        Thread.Sleep(MsDelay);
                    }

                    myIsGameRunning = false;
                });
        }

        public void Stop() {
            if (myGameTask == null)
                return;
            myCancellation.Cancel();
        }

        private int CountNeighbors(int rowInd, int colInd) {
            int maxCol = colInd == (NCells - 1) ? colInd : colInd + 1;
            int maxRow = rowInd == (NCells - 1) ? rowInd : rowInd + 1;
            int minCol = colInd == 0 ? colInd : colInd - 1;
            int minRow = rowInd == 0 ? rowInd : rowInd - 1;
            int c = 0;

            for (int row = minRow; row <= maxRow; ++row) {
                for (int col = minCol; col <= maxCol; ++col) {
                    if ((row == rowInd) && (col == colInd))
                        continue;

                    if (BoolField[row, col])
                        ++c;
                }
            }

            return c;
        }

        #region Events

        //------------------------------------------------------------------------------------------

        private void rect_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
            if (myIsGameRunning)
                return;

            Rectangle rect = sender as Rectangle;
            rect.Opacity = 0.5;
        }

        private void rect_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
            if (myIsGameRunning)
                return;

            Rectangle rect = sender as Rectangle;
            rect.Opacity = 1;
        }

        private void rect_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (myIsGameRunning)
                return;

            Rectangle rect = sender as Rectangle;
            bool found = false;

            for (int row = 0; row < NCells; ++row) {
                for (int col = 0; col < NCells; ++col) {
                    if (RectField[row, col].Equals(rect)) {
                        BoolField[row, col] = !BoolField[row, col];
                        found = true;
                        break;
                    }
                }

                if (found)
                    break;
            }

            if (found)
                Refresh();
        }

        //------------------------------------------------------------------------------------------

        #endregion Events

        #region Properties

        //------------------------------------------------------------------------------------------

        public Brush BackColor {
            get { return myBackColor; }
            set { myBackColor = value; }
        }

        public Brush BorderColor {
            get { return myBorderColor; }
            set { myBorderColor = value; }
        }

        public byte BorderSize {
            get { return myBorderSize; }
            set { myBorderSize = value; }
        }

        public Brush CellColor {
            get { return myCellColor; }
            set { myCellColor = value; }
        }

        public Canvas GameCanvas {
            get { return myGameCanvas; }
            set { myGameCanvas = value; }
        }

        public int MsDelay {
            get { return myMsDelay; }
            set { myMsDelay = value; }
        }

        public int NCells {
            get { return myNCells; }
            set {
                if (value <= 0)
                    value = 1;
                myNCells = value;
            }
        }

        //------------------------------------------------------------------------------------------

        #endregion Properties
    }
}