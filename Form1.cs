using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BridgePointsCounter
{
    public partial class Form1 : Form
    {
        bool IsOnline = true;
        List<RichTextBox> LtextBoxList;
        List<RichTextBox> RtextBoxList;
        GoogleWorksheet worksheet = new GoogleWorksheet();
        Dictionary<int, Tuple<bool, bool>> games = new Dictionary<int, Tuple<bool, bool>>()
        {
            { 0, new Tuple<bool, bool>( false, false ) },
            { 1, new Tuple<bool, bool>( false, true ) },
            { 2, new Tuple<bool, bool>( true, false ) },
            { 3, new Tuple<bool, bool>( true, true ) },
            { 4, new Tuple<bool, bool>( false, false ) },
            { 5, new Tuple<bool, bool>( false, true ) },
            { 6, new Tuple<bool, bool>( true, false ) },
            { 7, new Tuple<bool, bool>( true, true ) }
        };

        public Form1()
        {
            InitializeComponent();
            LtextBoxList = new List<RichTextBox> { L1TextBox, L2TextBox, L3TextBox, L4TextBox, L5TextBox, L6TextBox, L7TextBox, L8TextBox };
            RtextBoxList = new List<RichTextBox> { R1TextBox, R2TextBox, R3TextBox, R4TextBox, R5TextBox, R6TextBox, R7TextBox, R8TextBox };
            whoBox.SelectedIndex = 0;
            colorBox.SelectedIndex = 0;
            rowComboBox.SelectedIndex = FirstEmptyBox();
            foreach (RichTextBox box in LtextBoxList.Concat(RtextBoxList))
            {
                box.SelectionAlignment = HorizontalAlignment.Center;
            }
            LsumTextBox.SelectionAlignment = HorizontalAlignment.Center;
            RsumTextBox.SelectionAlignment = HorizontalAlignment.Center;

            foreach (RichTextBox box in LtextBoxList)
            {
                box.TextChanged += new EventHandler(LpointsChanged);
            }
            foreach (RichTextBox box in RtextBoxList)
            {
                box.TextChanged += new EventHandler(RpointsChanged);
            }

            LpointsChanged(null, null);
            RpointsChanged(null, null);
            try
            {
                CheckForUnfinishedGames();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                InformAboutConnectionError();
            }
            catch (Google.GoogleApiException)
            {
                InformAboutSpreadsheetError();
            }
            ShowRecentResults();
        }

        private void InformAboutSpreadsheetError()
        {
            string message = "Brak połączenia z chmurą. Kontynuować offline?";
            string caption = "Brak dostępu do danych";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            // Displays the MessageBox.

            result = MessageBox.Show(message, caption, buttons);

            if (result == DialogResult.No)
            {
                this.Close();
            }
            else if (result == DialogResult.Yes)
            {
                saveGame.Enabled = false;
                loadGame.Enabled = false;
                IsOnline = false;
            }
        }

        private void ShowRecentResults()
        {
            if (!IsOnline)
                return;
            List<Tuple<string, string>> recentResults = worksheet.LoadRecentResults();
            who1.Text = recentResults[0].Item1;
            who2.Text = recentResults[1].Item1;
            who3.Text = recentResults[2].Item1;
            who4.Text = recentResults[3].Item1;
            who5.Text = recentResults[4].Item1;
            howMuch1.Text = recentResults[0].Item2;
            howMuch2.Text = recentResults[1].Item2;
            howMuch3.Text = recentResults[2].Item2;
            howMuch4.Text = recentResults[3].Item2;
            howMuch5.Text = recentResults[4].Item2;
        }

        private void InformAboutConnectionError()
        {
            string message = "Aby korzystać z funkcji online uruchom ponownie aplikację z włączonym internetem. Kontynuować?";
            string caption = "Brak połączenia z internetem";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            // Displays the MessageBox.

            result = MessageBox.Show(message, caption, buttons);

            if (result == DialogResult.No)
            {
                this.Close();
            }
            else if (result == DialogResult.Yes)
            {
                saveGame.Enabled = false;
                loadGame.Enabled = false;
                IsOnline = false;
            }
        }

        private void CheckForUnfinishedGames()
        {
            if (!worksheet.IsPreviousGameFinished())
            {
                string message = "Poprzednia gra nie została zakończona. Czy chcesz ją wczytać?";
                string caption = "";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;

                // Displays the MessageBox.

                result = MessageBox.Show(message, caption, buttons);

                if (result == DialogResult.Yes)
                {
                    loadGame_Click(null, null);
                }
            }
        }

        private int FirstEmptyBox()
        {
            for(int i = 0; i<8; i++)
            {
                if (LtextBoxList[i].Text.Equals(string.Empty) && RtextBoxList[i].Text.Equals(string.Empty))
                    return i;
            }
            return -1;
        }

        private void LpointsChanged(object sender, EventArgs eventArgs)
        {
            try
            {
                int sum = 0;
                foreach(RichTextBox box in LtextBoxList)
                {
                    sum += int.Parse(box.Text.Equals(string.Empty) ? "0" : box.Text);
                }
                LsumTextBox.Text = "Suma: " + sum.ToString();
                rowComboBox.SelectedIndex = FirstEmptyBox();
            }
            catch(Exception)
            {
                LsumTextBox.Text = "Złe dane";
            }
        }

        private void RpointsChanged(object sender, EventArgs eventArgs)
        {
            try
            {
                int sum = 0;
                foreach (RichTextBox box in RtextBoxList)
                {
                    sum += int.Parse(box.Text.Equals(string.Empty) ? "0" : box.Text);
                }
                RsumTextBox.Text = "Suma: " + sum.ToString();
                rowComboBox.SelectedIndex = FirstEmptyBox();
            }
            catch (Exception)
            {
                RsumTextBox.Text = "Złe dane";
            }
        }

        private void submit_Click(object sender, EventArgs e)
        {
            int bid = (int)bidBox.Value;
            int scored = (int)scoredBox.Value;
            int pointsInPair = (int)pairPointsBox.Value;
            Enums.Colors color = GetColor(colorBox.Text);
            Enums.Doubles doubles = GetRadioButtonsState();
            int row = GetRow();
            if (row == -1)
                return;
            Tuple<bool, bool> afterGames = GetAfterGameValues(row, whoBox.Text);
            PointsCounter counter = new PointsCounter(bid, scored, pointsInPair, color, afterGames.Item1, afterGames.Item2, doubles);
            int score = counter.Calculate();
            PrintScore(score, row);
        }

        private void PrintScore(int score, int row)
        {
            if (score == 0)
            {
                LtextBoxList[row].Text = "0";
                RtextBoxList[row].Text = "0";
            }
            else if (whoBox.Text.Equals("My") && score > 0)
            {
                LtextBoxList[row].Text = score.ToString();
                RtextBoxList[row].Text = string.Empty;
            }
            else if (whoBox.Text.Equals("Oni") && score < 0)
            {
                LtextBoxList[row].Text = (-score).ToString();
                RtextBoxList[row].Text = string.Empty;
            }
            else if (whoBox.Text.Equals("Oni") && score > 0)
            {
                LtextBoxList[row].Text = string.Empty;
                RtextBoxList[row].Text = score.ToString();
            }
            else if (whoBox.Text.Equals("My") && score < 0)
            {
                LtextBoxList[row].Text = string.Empty;
                RtextBoxList[row].Text = (-score).ToString();
            }
        }

        private int GetRow()
        {
            try
            {
                return int.Parse(rowComboBox.Text) - 1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private Enums.Doubles GetRadioButtonsState()
        {
            if (brakKontryRadioButton.Checked)
                return Enums.Doubles.undoubled;
            else if (kontraRadioButton.Checked)
                return Enums.Doubles.doubled;
            else if (rekontraRadioButton.Checked)
                return Enums.Doubles.redoubled;
            else
                throw new ArgumentNullException();
        }

        /// <summary>
        /// returns afterGame state for both the player and enemies
        /// </summary>
        private Tuple<bool, bool> GetAfterGameValues(int row, string whoPlays)
        {
            Tuple<bool, bool> valuesInRow = games[row];
            if (whoPlays.Equals("My"))
                return valuesInRow;
            else if (whoPlays.Equals("Oni"))
                return new Tuple<bool, bool>(valuesInRow.Item2, valuesInRow.Item1);
            else
                throw new ArgumentException();
        }

        private Enums.Colors GetColor(string strColor)
        {
            if (strColor.Equals("Bez atu"))
                return Enums.Colors.notrumps;
            else if (strColor.Equals("Pik"))
                return Enums.Colors.spades;
            else if (strColor.Equals("Kier"))
                return Enums.Colors.hearts;
            else if (strColor.Equals("Karo"))
                return Enums.Colors.diamonds;
            else if (strColor.Equals("Trefl"))
                return Enums.Colors.clubs;
            else
                throw new ArgumentException();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            foreach (RichTextBox box in LtextBoxList.Concat(RtextBoxList))
            {
                box.Text = string.Empty;
            }
        }

        private async void loadGame_Click(object sender, EventArgs e)
        {
            List<List<string>> scoreboard = null;
            await Task.Run(() =>
            {
                scoreboard = worksheet.Load();
            });
            for (int i=0; i<8; i++)
            {
                LtextBoxList[i].Text = scoreboard[0][i];
            }
            for (int i = 0; i < 8; i++)
            {
                RtextBoxList[i].Text = scoreboard[1][i];
            }
        }

        private async void saveGame_Click(object sender, EventArgs e)
        {
            var packedData = PackScoreData();
            await Task.Run(() =>
            {
                worksheet.Save(packedData);
            });
            ShowRecentResults();
        }

        private List<List<object>> PackScoreData()
        {
            List<List<object>> score = new List<List<object>>()
            {
                new List<object>(),
                new List<object>()
            };
            score[0].Add("My");
            score[1].Add("Oni");
            for (int i = 0; i < 8; i++)
            {
                score[0].Add(LtextBoxList[i].Text);
            }
            for (int i = 0; i < 8; i++)
            {
                score[1].Add(RtextBoxList[i].Text);
            }
            return score;
        }
    }
}
