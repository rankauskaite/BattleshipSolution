using BattleshipClient.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BattleshipClient
{
    public enum CellState { Empty, Ship, Miss, Hit, Whole_ship_down, }

    public class GameBoard : Control
    {
        public const int Size = 10;
        public CellState[,] Cells = new CellState[Size, Size];
        public int CellPx { get; set; } = 36;
        public int LabelMargin { get; set; } = 25; // vieta raidėms/skaičiams

        public event EventHandler<Point> CellClicked;
        public event Action<ShipData, Point> ShipDropped; // ship + cell
        public List<ShipDto> Ships { get; set; } = new List<ShipDto>();


        public GameBoard()
        {
            this.DoubleBuffered = true;
            this.AllowDrop = true; // priima drag
            this.DragEnter += GameBoard_DragEnter;
            this.DragDrop += GameBoard_DragDrop;
            this.Width = CellPx * Size + LabelMargin + 1;
            this.Height = CellPx * Size + LabelMargin + 1;
        }

        public void SetCell(int x, int y, CellState state)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return;
            Cells[y, x] = state;
            Invalidate(new Rectangle(x * CellPx + LabelMargin, y * CellPx + LabelMargin, CellPx, CellPx));
        }

        public CellState GetCell(int x, int y)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return CellState.Empty;
            return Cells[y, x];
        }

        public void ClearBoard()
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    Cells[r, c] = CellState.Empty;
            Invalidate(); // perpiešti
        }

        private void GameBoard_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ShipData)))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void GameBoard_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ShipData)))
            {
                var ship = (ShipData)e.Data.GetData(typeof(ShipData));

                // konvertuoti ekrano koordinatę į lentos koordinatę (atsižvelgiant į margin)
                Point client = PointToClient(new Point(e.X, e.Y));
                int c = (client.X - LabelMargin) / CellPx;
                int r = (client.Y - LabelMargin) / CellPx;

                ShipDropped?.Invoke(ship, new Point(c, r));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(ColorTranslator.FromHtml("#f8f9fa"));

            using var font = new Font("Arial", 10, FontStyle.Bold);
            using var brush = new SolidBrush(Color.Black);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            // Piešti raides viršuje (A-J)
            for (int c = 0; c < Size; c++)
            {
                string letter = ((char)('A' + c)).ToString();
                var rect = new Rectangle(LabelMargin + c * CellPx, 0, CellPx, LabelMargin);
                g.DrawString(letter, font, brush, rect, sf);
            }

            // Piešti skaičius kairėje (1-10)
            for (int r = 0; r < Size; r++)
            {
                string number = (r + 1).ToString();
                var rect = new Rectangle(0, LabelMargin + r * CellPx, LabelMargin, CellPx);
                g.DrawString(number, font, brush, rect, sf);
            }

            // Piešti langelius
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    var rect = new Rectangle(LabelMargin + c * CellPx, LabelMargin + r * CellPx, CellPx, CellPx);
                    var state = Cells[r, c];
                    switch (state)
                    {
                        case CellState.Empty:
                            using (var b = new SolidBrush(ColorTranslator.FromHtml("#dbe9f7"))) g.FillRectangle(b, rect);
                            break;
                        case CellState.Ship:
                            using (var b = new SolidBrush(ColorTranslator.FromHtml("#6c757d"))) g.FillRectangle(b, rect);
                            break;
                        case CellState.Hit:
                            using (var b = new SolidBrush(ColorTranslator.FromHtml("#dc3545"))) g.FillRectangle(b, rect);
                            break;
                        case CellState.Miss:
                            using (var b = new SolidBrush(ColorTranslator.FromHtml("#ffffff"))) g.FillRectangle(b, rect);
                            break;
                        case CellState.Whole_ship_down:
                            using (var b = new SolidBrush(ColorTranslator.FromHtml("#781D26"))) g.FillRectangle(b, rect);
                            break;
                    }
                    g.DrawRectangle(Pens.Black, rect);
                }
            }

            using var pen = new Pen(Color.Black, 3);
            foreach (var ship in Ships)
            {
                int x = LabelMargin + ship.x * CellPx;
                int y = LabelMargin + ship.y * CellPx;
                int w = (ship.dir == "H" ? ship.len : 1) * CellPx;
                int h = (ship.dir == "V" ? ship.len : 1) * CellPx;
                g.DrawRectangle(pen, x, y, w, h);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int c = (e.X - LabelMargin) / CellPx;
            int r = (e.Y - LabelMargin) / CellPx;
            if (c >= 0 && c < Size && r >= 0 && r < Size)
                CellClicked?.Invoke(this, new Point(c, r));
        }
    }
}
