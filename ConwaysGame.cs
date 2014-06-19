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
        private Brush myBackColor;
        private bool[,] myBoolField;
        private Brush myBorderColor;
        private byte myBorderSize;
        private CancellationTokenSource myCancellation;
        private Brush myCellColor;
        private Canvas myGameCanvas;
        private Task myGameTask;
        private bool myIsGameRunning;
        private int myMsDelay;
        private int myNCells;
        private bool[,] myNewField;
        private int myNumberOfTasks;
        private ManualResetEvent mySignal;
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
            myBoolField = new bool[NCells, NCells];
        }

        public void DrawGrid() {
            RectField = new Rectangle[NCells, NCells];
            myBoolField = new bool[NCells, NCells];
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
            for (int row = 0; row < myBoolField.GetLength(0); ++row) {
                for (int col = 0; col < myBoolField.GetLength(0); ++col) {
                    if (myBoolField[row, col]) {
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

                if (myBoolField[row, col]) continue;

                myBoolField[row, col] = true;
                ++c;
            }
        }

        public void Start() {
            myCancellation = new CancellationTokenSource();
            CancellationToken ct = myCancellation.Token;
            myGameTask = Task.Factory.StartNew(
                () => {
                    myIsGameRunning = true;

                    while (!ct.IsCancellationRequested) {
                        myNewField = CopyBoolArray(myBoolField);
                        myNumberOfTasks = NCells * NCells;
                        mySignal = new ManualResetEvent(false);

                        for (int row = 0; row < NCells; ++row) {
                            for (int col = 0; col < NCells; ++col) {
                                ThreadPool.QueueUserWorkItem(CheckCell, new Tuple<int, int>(row, col));
                            }
                        }

                        mySignal.WaitOne();

                        myBoolField = myNewField;
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

        private void CheckCell(Object threadContext) {
            Tuple<int, int> tuple = threadContext as Tuple<int, int>;
            int row = tuple.Item1;
            int col = tuple.Item2;

            int neighbors = CountNeighbors(row, col);

            //- dead cell with three neighbors comes to life again
            if (!myBoolField[row, col] && (neighbors == 3))
                myNewField[row, col] = true;

            //- life cell with one or less neighbors dies
            else if (myBoolField[row, col] && (neighbors <= 1))
                myNewField[row, col] = false;

            //- life cell with more than three neighbors dies
            else if (myBoolField[row, col] && (neighbors > 3))
                myNewField[row, col] = false;

            if (Interlocked.Decrement(ref myNumberOfTasks) == 0)
                mySignal.Set();
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

                    if (myBoolField[row, col])
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
                        myBoolField[row, col] = !myBoolField[row, col];
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