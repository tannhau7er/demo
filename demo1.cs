using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using HtmlAgilityPack;
using Sniper.Properties;

namespace Eliteprospects
{
    public partial class Form1 : Form
    {
        private List<NameHrefObj> Teams;
        
        
        public Form1()
        {
            InitializeComponent();

            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;

            Teams = new List<NameHrefObj>();
        }


        private List<NameHrefObj> GetLeagues()
        {
            string pageSource = null;
            List<NameHrefObj> leaguesResult = new List<NameHrefObj>();

            using (WebClient web1 = new WebClient())
            {
                web1.Encoding = System.Text.Encoding.UTF8;
                pageSource = web1.DownloadString("https://www.eliteprospects.com/leagues");
            }

            if (pageSource == null)
                return null;

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(pageSource);

            HtmlNodeCollection leagueNodes = doc.DocumentNode.SelectNodes("//tr/td/ul[@class='list-unstyled']/li/ul/li/span/a");

            if (leagueNodes == null)
            {
                MessageBox.Show("Leagues nodes return null");
                return null;
            }

            string name = null, href = null;
            foreach(HtmlNode leagueNode in leagueNodes)
            {
                name = leagueNode.InnerText.Trim();
                href = leagueNode.Attributes["href"].Value;

                if (name == null || href == null)
                    continue;

                NameHrefObj league = new NameHrefObj();
                league.Name = name;
                league.Href = href;

                leaguesResult.Add(league);

                name = null;
                href = null;
            }

            if (leaguesResult.Count == 0)
                return null;

            return leaguesResult;
        }
        private List<NameHrefObj> GetTeams(int rowIndex)
        {
            string pageSource = null;
            List<NameHrefObj> teamsResult = new List<NameHrefObj>();

            using (WebClient web1 = new WebClient())
            {
                web1.Encoding = System.Text.Encoding.UTF8;
                pageSource = web1.DownloadString(dataGridViewLeagues.Rows[rowIndex].Cells[1].Value.ToString());
            }

            if (pageSource == null)
                return null;

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(pageSource);

            HtmlNodeCollection teamsNodes = doc.DocumentNode.SelectNodes("//div[@class='inner-rtl']/div[@class='leg-home-inner'][1]/div/ul/li/a");

            if(teamsNodes == null)
            {
                MessageBox.Show("Teams nodes return null");
                return null;
            }

            string name = null, href = null;
            foreach (HtmlNode teamNode in teamsNodes)
            {
                name = teamNode.InnerText.Trim();
                href = teamNode.Attributes["href"].Value;

                if (name == null || href == null)
                    continue;

                NameHrefObj team = new NameHrefObj();
                team.Name = name;
                team.Href = href;

                teamsResult.Add(team);

                name = null;
                href = null;
            }

            if (teamsResult.Count == 0)
                return null;

            return teamsResult;
        }
        private List<PlayerTableObj> GetPlayers(int teamIndex)
        {
            string pageSource = null;
            List<PlayerTableObj> playersResult = new List<PlayerTableObj>();

            using (WebClient web1 = new WebClient())
            {
                web1.Encoding = System.Text.Encoding.UTF8;
                pageSource = web1.DownloadString(Teams[teamIndex].Href);
            }

            if (pageSource == null)
                return null;

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(pageSource);

            HtmlNodeCollection playersNodes = doc.DocumentNode.SelectNodes("//div[@id='roster']/div/div[3]/table/tbody/tr/td[4]/span/a[1]");

            if (playersNodes == null)
            {
                MessageBox.Show("Players nodes return null");
                return null;
            }

            string name = null, href = null;
            foreach (HtmlNode playerNode in playersNodes)
            {
                name = playerNode.InnerText.Trim();
                href = playerNode.Attributes["href"].Value;

                if (name == null || href == null)
                    continue;

                PlayerTableObj team = new PlayerTableObj();
                team.Name = name;
                team.Href = href;

                playersResult.Add(team);

                name = null;
                href = null;
            }

            if (playersResult.Count == 0)
                return null;

            return playersResult;
        }
        private List<PlayerDataObj> GetPlayersData(List<PlayerTableObj> players)
        {
            List<PlayerDataObj> playersDataResult = new List<PlayerDataObj>();

            foreach(PlayerTableObj player in players)
            {
                PlayerDataObj playerData = new PlayerDataObj();
                string pageSource = null;

                using (WebClient web1 = new WebClient())
                {
                    web1.Encoding = System.Text.Encoding.UTF8;
                    pageSource = web1.DownloadString(player.Href);
                }

                if (pageSource == null)
                    continue;

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(pageSource);

                DateTime now = DateTime.Now;
                int year = Int32.Parse(now.ToString("yy"));

                HtmlNodeCollection GP_nodes = doc.DocumentNode.SelectNodes($"//div[@id='league-stats']/div[2]/table/tbody/tr/td[1]/span[contains(text(), '{year}') or contains(text(), '{year + 1}') or contains(text(), '{year - 1}')]/../../td[2]/span/a[not(contains(text(), 'Projected'))]/../../../td[4]");
                HtmlNodeCollection TP_nodes = doc.DocumentNode.SelectNodes($"//div[@id='league-stats']/div[2]/table/tbody/tr/td[1]/span[contains(text(), '{year}') or contains(text(), '{year + 1}') or contains(text(), '{year - 1}')]/../../td[2]/span/a[not(contains(text(), 'Projected'))]/../../../td[7]");

                if (GP_nodes == null || TP_nodes == null)
                    continue;

                float sumGP = 0f, sumTP = 0f;
                foreach (HtmlNode gpNode in GP_nodes)
                {
                    float gp = 0;
                    float.TryParse(gpNode.InnerText, out gp);
                    sumGP += gp;
                }
                foreach (HtmlNode tpNode in TP_nodes)
                {
                    float tp = 0;
                    float.TryParse(tpNode.InnerText, out tp);
                    sumTP += tp;
                }

                if (sumGP == 0f || sumTP == 0f)
                    continue;

                float sumTP_sumGP = sumTP / sumGP;
                float k_4 = player.K / 4f;
                float difference = sumTP_sumGP - k_4;

                playerData.Name = player.Name;
                playerData.K = player.K;
                playerData.SumTP_SumGP = sumTP_sumGP;
                playerData.K_4 = k_4;
                playerData.Difference = difference;

                playersDataResult.Add(playerData);
            }

            return playersDataResult;
        }



        private void Form1_Shown(object sender, EventArgs e)
        {
            List<NameHrefObj> res = GetLeagues();
            dataGridViewLeagues.DataSource = res;
            dataGridViewLeagues.Columns[1].Visible = false;
        }
        private void dataGridViewLeagues_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            comboBoxTeamLeft.Items.Clear();
            comboBoxTeamRight.Items.Clear();
            dataGridViewPlayersLeft.DataSource = null;
            dataGridViewPlayersRight.DataSource = null;
            Teams.Clear();

            Teams = GetTeams(e.RowIndex);

            foreach(NameHrefObj team in Teams)
            {
                comboBoxTeamLeft.Items.Add(team.Name);
                comboBoxTeamRight.Items.Add(team.Name);
            }

            tabControl1.SelectedIndex = 1;

            this.Cursor = Cursors.Default;
        }
        private void comboBoxTeamLeft_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            List<PlayerTableObj> players = GetPlayers(comboBoxTeamLeft.SelectedIndex);
            dataGridViewPlayersLeft.DataSource = null;
            dataGridViewPlayersLeft.DataSource = players;
            dataGridViewPlayersLeft.Columns[1].Visible = false;

            this.Cursor = Cursors.Default;
        }
        private void comboBoxTeamRight_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            List<PlayerTableObj> players = GetPlayers(comboBoxTeamRight.SelectedIndex);
            dataGridViewPlayersRight.DataSource = null;
            dataGridViewPlayersRight.DataSource = players;
            dataGridViewPlayersRight.Columns[1].Visible = false;

            this.Cursor = Cursors.Default;
        }
        private void buttonBack_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 0;
        }
        private void buttonNext_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(comboBoxTeamLeft.Text) || string.IsNullOrEmpty(comboBoxTeamRight.Text))
            {
                MessageBox.Show("Выберите команды!");
                return;
            }

            List<PlayerTableObj> leftPlayers = dataGridViewPlayersLeft.DataSource as List<PlayerTableObj>;
            leftPlayers = leftPlayers.Where(x => x.Checked == true).ToList();
            
            List<PlayerTableObj> rightPlayers = dataGridViewPlayersRight.DataSource as List<PlayerTableObj>;
            rightPlayers = rightPlayers.Where(x => x.Checked == true).ToList();

            if(leftPlayers.Count == 0 || rightPlayers.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одного игрока в каждой команде!");
                return;
            }

            foreach(PlayerTableObj player in leftPlayers)
            {
                if(player.K <= 0f)
                {
                    MessageBox.Show("Коэффициенты должны быть больше нуля!");
                    return;
                }
            }
            foreach (PlayerTableObj player in rightPlayers)
            {
                if (player.K <= 0f)
                {
                    MessageBox.Show("Коэффициенты должны быть больше нуля!");
                    return;
                }
            }

            this.Cursor = Cursors.WaitCursor;

            List<PlayerDataObj> leftPlayersData = GetPlayersData(leftPlayers);
            List<PlayerDataObj> rightPlayersData = GetPlayersData(rightPlayers);

            if(leftPlayersData.Count == 0 || rightPlayersData.Count == 0)
            {
                MessageBox.Show("Данные одной из команд не были получены!");
                this.Cursor = Cursors.Default;
                return;
            }

            dataGridViewLeftResult.DataSource = leftPlayersData;
            dataGridViewRightResult.DataSource = rightPlayersData;

            tabControl1.SelectedIndex = 2;

            labelLeftTeamName.Text = comboBoxTeamLeft.Text;
            labelRightTeamName.Text = comboBoxTeamRight.Text;

            this.Cursor = Cursors.Default;
        }
        private void buttonBack2_Click(object sender, EventArgs e)
        {
            dataGridViewLeftResult.DataSource = null;
            dataGridViewRightResult.DataSource = null;

            tabControl1.SelectedIndex = 1;
        }



        Rectangle ImageArea(PictureBox pbox)
        {
            Size si = pbox.Image.Size;
            Size sp = pbox.ClientSize;
            float ri = 1f * si.Width / si.Height;
            float rp = 1f * sp.Width / sp.Height;
            if (rp > ri)
            {
                int width = si.Width * sp.Height / si.Height;
                int left = (sp.Width - width) / 2;
                return new Rectangle(left, 0, width, sp.Height);
            }
            else
            {
                int height = si.Height * sp.Width / si.Width;
                int top = (sp.Height - height) / 2;
                return new Rectangle(0, top, sp.Width, height);
            }
        }



        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            pictureBox1.Cursor = ImageArea(pictureBox1).Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }
        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            pictureBox1.Image = Resources.vk_hover;
        }
        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            pictureBox1.Image = Resources.vk;
        }


        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            pictureBox2.Cursor = ImageArea(pictureBox2).Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }
        private void pictureBox2_MouseEnter(object sender, EventArgs e)
        {
            pictureBox2.Image = Resources.telegram_hover;
        }
        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            pictureBox2.Image = Resources.telegram;
        }


        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://vk.com/raduzhnyi_poni") { UseShellExecute = true });
        }
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://t.me/chack_frozzy") { UseShellExecute = true });
        }
    }



    internal class NameHrefObj
    {
        public string Name { get; set; }
        public string Href { get; set; }
    }
    internal class PlayerTableObj
    {
        public string Name { get; set; }
        public string Href { get; set; }
        public bool Checked { get; set; } = false;
        public float K { get; set; } = 0f;
    }
    internal class PlayerDataObj
    {
        public string Name { get; set; }
        public float K { get; set; }
        public float SumTP_SumGP { get; set; }
        public float K_4 { get; set; }
        public float Difference { get; set; }
    }
}
