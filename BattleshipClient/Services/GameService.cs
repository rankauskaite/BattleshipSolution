
namespace BattleshipClient.Services
{
    class GameService
    {
        public void ResetMyFormOnly(MainForm form, bool _btn_ready_enabled, bool _placing_ships = false, bool _ship_panel_visible = false)
        {
            form.ownBoard.ClearBoard();
            form.myShips.Clear();
            form.shipPanel.Controls.Clear();
            form.shipPanel.Visible = _ship_panel_visible;
            form.btnReady.Enabled = _btn_ready_enabled;
            form.placingShips = _placing_ships;

        }

        public async void ResetForm(MainForm form, bool _btn_ready_enabled, bool clearEnemyBoard = true, bool _placing_ships = false, bool _ship_panel_visible = false)
        {
            ResetMyFormOnly(form, _btn_ready_enabled, _placing_ships, _ship_panel_visible);
            if (clearEnemyBoard)
                form.enemyBoard.ClearBoard();
            form.isMyTurn = false;

            form.lblStatus.Text = "Waiting for new game...";
            var register = new { type = "register", payload = new { playerName = form.txtName.Text } };
            await form.net.SendAsync(register);
        }
    }
}
