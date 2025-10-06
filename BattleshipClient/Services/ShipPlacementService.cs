using BattleshipClient.Models;

namespace BattleshipClient.Services
{
    class ShipPlacementService
    {
        private int[] lens { get; } = new int[] { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };

        public ShipPlacementService()
        {
        }

        public ShipPlacementService(int[] lens)
        {
            this.lens = lens;
        }

        public (List<ShipDto> ships, CellState[,] map) RandomizeShips()
        {
            var rnd = new Random();
            List<ShipDto> ships = new List<ShipDto>();
            var temp = new CellState[GameBoard.Size, GameBoard.Size];

            foreach (var len in lens)
            {
                bool placed = false;
                int tries = 0;
                while (!placed && tries < 200)
                {
                    tries++;
                    bool horiz = rnd.Next(2) == 0;
                    int x = rnd.Next(0, GameBoard.Size - (horiz ? len - 1 : 0));
                    int y = rnd.Next(0, GameBoard.Size - (horiz ? 0 : len - 1));
                    bool ok = true;
                    for (int i = 0; i < len; i++)
                    {
                        int cx = x + (horiz ? i : 0);
                        int cy = y + (horiz ? 0 : i);
                        if (temp[cy, cx] != CellState.Empty) { ok = false; break; }
                    }
                    if (ok)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            int cx = x + (horiz ? i : 0);
                            int cy = y + (horiz ? 0 : i);
                            temp[cy, cx] = CellState.Ship;
                        }
                        ships.Add(new ShipDto { x = x, y = y, len = len, dir = horiz ? "H" : "V" });
                        placed = true;
                    }
                }
            }

            return (ships, temp);
        }

        public static bool CanPlaceShip(GameBoard board, int x, int y, int len, bool horiz)
        {
            if (horiz && x + len > GameBoard.Size) return false;
            if (!horiz && y + len > GameBoard.Size) return false;

            for (int i = 0; i < len; i++)
            {
                int cx = x + (horiz ? i : 0);
                int cy = y + (horiz ? 0 : i);
                if (board.GetCell(cx, cy) != CellState.Empty) return false;
            }
            return true;
        }

        public static void PlaceShip(GameBoard board, int x, int y, int len, bool horiz)
        {
            for (int i = 0; i < len; i++)
            {
                int cx = x + (horiz ? i : 0);
                int cy = y + (horiz ? 0 : i);
                board.SetCell(cx, cy, CellState.Ship);
            }
        }

        public void HandlePlaceShip(bool placingHorizontal, FlowLayoutPanel shipPanel)
        {
            foreach (var len in this.lens)
            {
                var preview = new ShipPreviewControl(len) { Horizontal = placingHorizontal };
                preview.MouseDown += (s, ev) =>
                {
                    if (ev.Button == MouseButtons.Left)
                    {
                        var p = (ShipPreviewControl)s;
                        var data = new ShipData { Id = p.Id, Length = p.Length, Horizontal = p.Horizontal };
                        p.DoDragDrop(data, DragDropEffects.Copy);
                    }
                };
                shipPanel.Controls.Add(preview);
            }
        }

        public (bool success, int shipNo) RemoveShip(List<ShipDto> ships, GameBoard board, FlowLayoutPanel shipPanel, Point p)
        {
            foreach (var s in ships.ToList())
            {
                int x = s.x, y = s.y, len = s.len;
                bool horiz = s.dir == "H";
                for (int i = 0; i < len; i++)
                {
                    int cx = x + (horiz ? i : 0);
                    int cy = y + (horiz ? 0 : i);
                    if (p.X == cx && p.Y == cy)
                    {
                        for (int j = 0; j < len; j++)
                        {
                            int px = x + (horiz ? j : 0);
                            int py = y + (horiz ? 0 : j);
                            board.SetCell(px, py, CellState.Empty);
                        }

                        var preview = new ShipPreviewControl(len) { Horizontal = horiz };
                        preview.MouseDown += (s, ev) =>
                        {
                            if (ev.Button == MouseButtons.Left)
                            {
                                var p2 = (ShipPreviewControl)s;
                                var data = new ShipData { Id = p2.Id, Length = p2.Length, Horizontal = p2.Horizontal };
                                p2.DoDragDrop(data, DragDropEffects.Copy);
                            }
                        };
                        shipPanel.Controls.Add(preview);
                        shipPanel.Visible = true;

                        ships.Remove(s);
                        board.Ships = ships;
                        board.Invalidate();
                        return (true, len);
                    }
                }
            }
            return (false, -1);
        }
    }
}
