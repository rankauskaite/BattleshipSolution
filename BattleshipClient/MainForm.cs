using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleshipClient.Models;
using BattleshipClient.Services;

namespace BattleshipClient
{
    public class MainForm : Form
    {
        private TextBox txtServer;
        public TextBox txtName { get; private set; }
        private Button btnConnect;
        private Button btnRandomize;
        public Button btnReady;
        private Button btnPlaceShips;
        public Button btnGameOver; // Naujas mygtukas
        public Label lblStatus;
        public GameBoard ownBoard { get; set; }
        public GameBoard enemyBoard { get; set; }
        public FlowLayoutPanel shipPanel;

        public NetworkClient net { get; private set; } = new NetworkClient();

        // state
        public List<ShipDto> myShips { get; private set; } = new List<ShipDto>();
        public bool isMyTurn = false;
        public string myId { get; set; } = "";
        public string oppId { get; set; } = "";

        // drag & drop state
        public bool placingShips = false;
        private bool placingHorizontal = true;

        // services
        private ShipPlacementService ShipPlacementService = new ShipPlacementService();
        private GameService GameService = new GameService();
        private MessageService MessageService = new MessageService();

        public List<ShipDto> Ships { get; set; } = new List<ShipDto>();

        public MainForm()
        {
            InitializeComponents();
            net.OnMessageReceived += Net_OnMessageReceived;
            ownBoard.ShipDropped += OwnBoard_ShipDropped;
            ownBoard.CellClicked += OwnBoard_CellClickedForRemoval;
            btnGameOver.Click += BtnGameOver_Click;
        }

        private void InitializeComponents()
        {
            this.Text = "Battleship Client";
            this.ClientSize = new Size(950, 600);
            this.BackColor = ColorTranslator.FromHtml("#f8f9fa");

            Label l1 = new Label { Text = "Server (ws):", Location = new Point(10, 10), AutoSize = true };
            txtServer = new TextBox { Text = "ws://localhost:5000/ws/", Location = new Point(100, 10), Width = 300 };
            Label l2 = new Label { Text = "Name:", Location = new Point(420, 10), AutoSize = true };
            txtName = new TextBox { Text = "Player", Location = new Point(470, 10), Width = 120 };

            btnConnect = new Button { Text = "Connect", Location = new Point(600, 8), Width = 80, Height = 30 };
            btnConnect.Click += BtnConnect_Click;

            btnRandomize = new Button { Text = "Randomize ships", Location = new Point(700, 8), Width = 130, Height = 30 };
            btnRandomize.Click += BtnRandomize_Click;

            btnPlaceShips = new Button { Text = "Place ships", Location = new Point(560, 44), Width = 130, Height = 30 };
            btnPlaceShips.Click += BtnPlaceShips_Click;

            btnReady = new Button { Text = "Ready", Location = new Point(700, 44), Width = 130, Height = 30 };
            btnReady.Click += BtnReady_Click;

            btnGameOver = new Button { Text = "Game Over", Location = new Point(400, 44), Width = 100, Height = 30, Visible = false };

            lblStatus = new Label { Text = "Not connected", Location = new Point(10, 40), AutoSize = true };

            ownBoard = new GameBoard { Location = new Point(10, 80) };
            enemyBoard = new GameBoard { Location = new Point(480, 80) };
            enemyBoard.CellClicked += EnemyBoard_CellClicked;

            shipPanel = new FlowLayoutPanel
            {
                Location = new Point(30, 480),
                Size = new Size(450, 100),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            this.Controls.Add(shipPanel);
            this.Controls.AddRange(new Control[] {
                l1, txtServer, l2, txtName,
                btnConnect, btnRandomize, btnPlaceShips, btnReady, btnGameOver,
                lblStatus, ownBoard, enemyBoard
            });

            btnReady.Enabled = false;
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                await net.ConnectAsync(txtServer.Text);
                lblStatus.Text = "Connected.";
                var register = new { type = "register", payload = new { playerName = txtName.Text } };
                await net.SendAsync(register);
                btnConnect.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connect failed: " + ex.Message);
            }
        }

        private void BtnPlaceShips_Click(object sender, EventArgs e)
        {
            this.GameService.ResetMyFormOnly(this, myShips.Count == 10, true, true);

            this.ShipPlacementService.HandlePlaceShip(placingHorizontal, shipPanel);

            lblStatus.Text = "Drag ships onto your board. Use 'R' to rotate before dragging.";
        }

        private void OwnBoard_ShipDropped(ShipData ship, Point cell)
        {
            if (!placingShips) return;
            int x = cell.X;
            int y = cell.Y;

            if(!ShipPlacementService.CanPlaceShip(ownBoard, x, y, ship.Length, ship.Horizontal))
            {
                MessageBox.Show("Invalid placement here.");
                return;
            }

            ShipPlacementService.PlaceShip(ownBoard, x, y, ship.Length, ship.Horizontal);

            myShips.Add(new ShipDto
            {
                x = x,
                y = y,
                len = ship.Length,
                dir = ship.Horizontal ? "H" : "V"
            });
            ownBoard.Ships = myShips;
            ownBoard.Invalidate();
            btnReady.Enabled = myShips.Count == 10;

            var ctrl = shipPanel.Controls.Cast<Control>().FirstOrDefault(c => c.Tag is Guid g && g == ship.Id);
            if (ctrl != null) shipPanel.Controls.Remove(ctrl);
            if (shipPanel.Controls.Count == 0) shipPanel.Visible = false;

            lblStatus.Text = $"Placed {ship.Length}-cell ship at {x},{y} ({(ship.Horizontal ? "H" : "V")}).";
        }

        private void OwnBoard_CellClickedForRemoval(object sender, Point p)
        {
            (bool successful_removal, int len) = this.ShipPlacementService.RemoveShip(this.myShips, this.ownBoard, this.shipPanel, p);
            if (successful_removal && len >= 0)
            {
                btnReady.Enabled = myShips.Count == 10;
                lblStatus.Text = $"Removed {len}-cell ship from board.";
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.R && placingShips)
            {
                placingHorizontal = !placingHorizontal;

                foreach (ShipPreviewControl sp in shipPanel.Controls.OfType<ShipPreviewControl>())
                {
                    sp.Horizontal = placingHorizontal;
                    sp.Width = placingHorizontal ? sp.Length * 30 : 30;
                    sp.Height = placingHorizontal ? 30 : sp.Length * 30;
                    sp.Invalidate();
                }

                lblStatus.Text = $"Orientation changed to {(placingHorizontal ? "H" : "V")}";
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void EnemyBoard_CellClicked(object sender, Point p)
        {
            if (!isMyTurn) { lblStatus.Text = "Not your turn."; return; }
            lblStatus.Text = $"Firing at {p.X},{p.Y}...";
            var shot = new { type = "shot", payload = new { x = p.X, y = p.Y } };
            await net.SendAsync(shot);
        }

        private void BtnRandomize_Click(object sender, EventArgs e)
        {
            myShips.Clear();
            (myShips, CellState[,] temp) = ShipPlacementService.RandomizeShips();
            ownBoard.Ships = myShips;
            ownBoard.Invalidate();
            btnReady.Enabled = myShips.Count == 10;

            for (int r = 0; r < GameBoard.Size; r++)
                for (int c = 0; c < GameBoard.Size; c++)
                    ownBoard.SetCell(c, r, temp[r, c]);

            lblStatus.Text = $"Randomized {myShips.Count} ships.";
        }

        private async void BtnReady_Click(object sender, EventArgs e)
        {
            if (myShips.Count != 10)
            {
                MessageBox.Show("You must place all ships before pressing Ready.");
                return;
            }

            var payload = new { ships = myShips };
            var msg = new { type = "ready", payload = payload };
            await net.SendAsync(msg);
            lblStatus.Text = "Ready sent.";
            btnReady.Enabled = false;
            btnPlaceShips.Enabled = false;
            btnRandomize.Enabled = false;
        }

        private void Net_OnMessageReceived(MessageDto dto)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Net_OnMessageReceived(dto)));
                return;
            }
            this.MessageService.HandleMessage(dto, this);
        }

        private async void BtnGameOver_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Do you want to play again?", "Game Over", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                this.GameService.ResetForm(this, false);
            }
            else
            {
                this.Close();
            }

            btnGameOver.Visible = false;
        }
    }
}
