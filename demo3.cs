using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HtmlAgilityPack;
using Kolarov.Properties;
using NCalc;

namespace Kolarov
{
    public partial class Form1 : Form
    {
        public List<Tuple<string, string>> _Clubs_list = new List<Tuple<string, string>>();
        public List<Tuple<string, string>> _Players_list = new List<Tuple<string, string>>();
        public Form1()
        {
            InitializeComponent();
        }

        // Get webpagecode
        public string DownloadString(string address)
        {
            WebClient client = new WebClient();
            client.UseDefaultCredentials = true;
            client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.71 Safari/537.36.");
            string reply = client.DownloadString(address);

            return reply;
        }
        // Get Club names
        public void GetClubs(string htmlpage, int site_index)
        {
            _Clubs_list.Clear();

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlpage);
            HtmlNodeCollection nodes_name = null;
            HtmlNodeCollection nodes_url = null;

            switch (site_index)
            {
                case 0:
                    nodes_url = doc.DocumentNode.SelectNodes("//dd[@class='b-details_txt']/div[@class='m-clear']/a[2]");
                    nodes_name = doc.DocumentNode.SelectNodes("//h5[@class='e-club_name']/a");
                    for (int i = 0; i < nodes_name.Count(); i++)
                    {
                        string input = nodes_url[i].GetAttributeValue("href", string.Empty);
                        string pattern = @"\ball\b";
                        string replace = "skaters";
                        string result = Regex.Replace(input, pattern, replace);
                        _Clubs_list.Add(new Tuple<string, string>(nodes_name[i].InnerText, result));
                    }
                    break;
                case 1:
                    nodes_url = doc.DocumentNode.SelectNodes("//div[@class='clubs__list']/ul/li/a");
                    nodes_name = doc.DocumentNode.SelectNodes("//div[@class='clubs__list']/ul/li/a/div/div[2]/p/strong");
                    for (int i = 0; i < nodes_name.Count(); i++)
                    {
                        _Clubs_list.Add(new Tuple<string, string>(nodes_name[i].InnerText, nodes_url[i].GetAttributeValue("href", string.Empty)));
                    }
                    break;
                case 2:
                    nodes_name = doc.DocumentNode.SelectNodes("//table[@class='site_table site_table-denser']//tbody/tr/td[3]/a");
                    for (int i = 0; i < nodes_name.Count(); i++)
                    {
                        _Clubs_list.Add(new Tuple<string, string>(nodes_name[i].InnerText, nodes_name[i].GetAttributeValue("href", string.Empty)));
                    }
                    break;
                case 5:
                    nodes_name = doc.DocumentNode.SelectNodes("//table[@class='table standings table-sortable']/tbody/tr/td[2]/a");
                    break;
            }
        }
        // Fill Table
        public void TableFill()
        {
            dataGridView1.DataSource = null;

            string[] site_arr = new string[]
            {
                "https://www.khl.ru/clubs/",
                "https://www.vhlru.ru/teams/",
                "https://mhl.khl.ru/stat/teams/",
                "https://www.naturalstattrick.com/teamtable.php",
                "https://www.championat.com/hockey/_superleague/tournament/4449/teams/",
                "https://www.eliteprospects.com/leagues"
            };

            _Clubs_list.Clear();

            if (comboBox1.SelectedIndex >= 0)
            {
                if (comboBox1.SelectedIndex < 3)
                {
                    string result = DownloadString(site_arr[comboBox1.SelectedIndex]);
                    GetClubs(result, comboBox1.SelectedIndex);

                    if (result != null)
                    {
                        DataTable teams_table = new DataTable();
                        teams_table.Columns.Add("Команда", typeof(string));

                        if (_Clubs_list.Count() > 0)
                        {
                            foreach (Tuple<string, string> name_url in _Clubs_list)
                            {
                                DataRow row = teams_table.NewRow();
                                row["Команда"] = name_url.Item1;
                                teams_table.Rows.Add(row);

                            }
                            
                            dataGridView1.DataSource = teams_table.DefaultView;
                        }
                    }
                }
                else if(comboBox1.SelectedIndex == 5)
                {
                    string[] league_arr = new string[]
                    {
                        "SHL Швеция",
                        "HockeyAllsvenskan Швеция",
                        "NL Швейцария",
                        "KHL Россия",
                        "VHL Россия",
                        "MHL Россия",
                        "AlpsHL",
                        "DEL Германия",
                        "DEL2 Германия",
                        "Tipsport Extraliga Чехия",
                        "Tipos Extraliga Словакия",
                        "ICEHL Австрия",
                        "Ligue Magnus Франция",
                        "Liiga Финляндия",
                        "Mestis Финляндия",
                        "Metal Ligaen Дания",
                        "EIHL Великобритания",
                        "Polska Hokej Liga Польша",
                        "Fjordkraft-ligaen, Норвегия",
                        "Erste Liga Венгрия"
                    };

                    DataTable leagues_table = new DataTable();
                    leagues_table.Columns.Add("Лига", typeof(string));

                    foreach (string league in league_arr)
                    {
                        DataRow row = leagues_table.NewRow();
                        row["Лига"] = league;
                        leagues_table.Rows.Add(row);
                    }

                    dataGridView1.DataSource = leagues_table;
                }
            }
        }
        // Get khl players
        public void GetKHLPlayers(int index)
        {
            _Players_list.Clear();
            string html = DownloadString("https://www.khl.ru" + _Clubs_list[index].Item2);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode nodes_players_names = doc.DocumentNode.SelectSingleNode("//div[@class='b-data_row']/script");
            string pre_result = nodes_players_names.InnerHtml;

            HtmlAgilityPack.HtmlDocument doc2 = new HtmlAgilityPack.HtmlDocument();
            doc2.LoadHtml(pre_result);
            HtmlNodeCollection players_names = doc2.DocumentNode.SelectNodes("//a[contains(@href, '/players')]/b");
            HtmlNodeCollection players_url = doc2.DocumentNode.SelectNodes("//a[contains(@href, '/players')]");

            for(int i = 0; i < players_names.Count(); i++)
            {
                _Players_list.Add(new Tuple<string, string>(players_names[i].InnerText, players_url[i].GetAttributeValue("href", string.Empty)));
            }
        }
        // Fill Players KHL
        public void FillPlayers()
        {
            dataGridView1.DataSource = null;

            if(_Players_list.Count() > 0)
            {
                DataTable players_table = new DataTable();
                players_table.Columns.Add("Игрок", typeof(string));

                if (_Clubs_list.Count() > 0)
                {
                    foreach (Tuple<string, string> name_url in _Players_list)
                    {
                        DataRow row = players_table.NewRow();
                        row["Игрок"] = name_url.Item1;
                        players_table.Rows.Add(row);

                    }

                    dataGridView1.DataSource = players_table.DefaultView;
                }
            }
        }
        // Get KHL Player Stats and fill table
        public void GetKHLPlayerStatAndFill(int index)
        {
            dataGridView1.DataSource = null;

            string url = "https://www.khl.ru" + _Players_list[index].Item2;
            string html = DownloadString(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            
            HtmlNodeCollection nodes_stat_headers = doc.DocumentNode.SelectNodes("//table[@id='pl_Stats']/thead/tr/th");
            HtmlNodeCollection nodes_stat_data_tr = doc.DocumentNode.SelectNodes("//table[@id='pl_Stats']/tbody/tr");

            // Create columns
            DataTable player_stat_table = new DataTable();
            if (nodes_stat_headers != null)
            {
                foreach (HtmlNode header in nodes_stat_headers)
                {
                    string head = header.InnerText.Trim().Replace("+", "plus");
                    head = head.Replace("-", "minus");
                    head = head.Replace("/", "|");
                    player_stat_table.Columns.Add(head, typeof(string));
                }
            }

            // Fill Table
            if (nodes_stat_data_tr != null)
            {
                foreach (HtmlNode statRow in nodes_stat_data_tr)
                {
                    HtmlNodeCollection rowStats_arr = statRow.SelectNodes("./td");
                    DataRow row = player_stat_table.NewRow();

                    for (int i = 0; i < player_stat_table.Columns.Count; i++)
                    {
                        row[player_stat_table.Columns[i].ColumnName] = rowStats_arr[i].InnerText.Trim();
                    }
                    player_stat_table.Rows.Add(row);
                }
            }

            dataGridView1.DataSource = player_stat_table.DefaultView;

            // Set Columns size
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                if (i == 0)
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                else
                {
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dataGridView1.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
        }
        // Get VHL players
        public void GetVHLPlayers(int index)
        {
            _Players_list.Clear();

            string html = DownloadString("https://www.vhlru.ru" + _Clubs_list[index].Item2);
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection nodes_players = doc.DocumentNode.SelectNodes("//table[@class='team_table']/tbody/tr/td/a");

            if (nodes_players != null)
            {
                for (int i = 0; i < nodes_players.Count(); i++)
                {
                    _Players_list.Add(new Tuple<string, string>(nodes_players[i].InnerText, nodes_players[i].GetAttributeValue("href", string.Empty)));
                }
            }

        }
        // Get VHL Player Stats and fill table
        public void GetVHLPlayerStatAndFill(int index)
        {
            dataGridView1.DataSource = null;

            string url = "https://www.vhlru.ru" + _Players_list[index].Item2;
            string html = DownloadString(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            HtmlNodeCollection nodes_stat_headers = doc.DocumentNode.SelectNodes("//div[@class='inner-tabs__content show']/div[1]/div/table/thead/tr/th");
            HtmlNodeCollection nodes_stat_data_tr = doc.DocumentNode.SelectNodes("//div[@class='inner-tabs__content show']/div[1]/div/table/tbody/tr");

            // Create columns
            DataTable player_stat_table = new DataTable();
            if (nodes_stat_headers != null)
            {
                foreach (HtmlNode header in nodes_stat_headers)
                {
                    string name = header.InnerText;
                    name = name.Replace("/", "|");
                    name = name.Replace("&quote;", "");
                    name = name.Replace("-", "minus");
                    name = name.Replace("+", "plus");

                    player_stat_table.Columns.Add(name, typeof(string));
                }
            }

            // Fill Table
            if (nodes_stat_data_tr != null)
            {
                foreach (HtmlNode statRow in nodes_stat_data_tr)
                {
                    HtmlNodeCollection rowStats_arr = statRow.SelectNodes("./td");
                    DataRow row = player_stat_table.NewRow();

                    for (int i = 0; i < player_stat_table.Columns.Count; i++)
                    {
                        row[player_stat_table.Columns[i].ColumnName] = rowStats_arr[i].InnerText.Trim();
                    }
                    player_stat_table.Rows.Add(row);
                }
            }

            dataGridView1.DataSource = player_stat_table.DefaultView;

            // Set Columns size
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                if (i == 0)
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                else
                {
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dataGridView1.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
        }
        // Get MHL Players
        public void GetMHLPlayers(int index)
        {
            _Players_list.Clear();

            string html = DownloadString("https://mhl.khl.ru" + _Clubs_list[index].Item2 + "teamplayers/");
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            
            HtmlNodeCollection players_name = doc.DocumentNode.SelectNodes("//div[@class='b_team_pic']/div");
            HtmlNodeCollection players_url = doc.DocumentNode.SelectNodes("//div[@class='b_team_pic']/a");
            
            if (players_name != null)
            {
                for (int i = 0; i < players_name.Count(); i++)
                {
                    string name = players_name[i].InnerText;
                    name = name.Remove(0, name.IndexOf(' ') + 1);
                    _Players_list.Add(new Tuple<string, string>(name, players_url[i].GetAttributeValue("href", string.Empty)));
                }
            }
        }
        // Get MHL Player Stats and fill table
        public void GetMHLPlayerStatAndFill(int index)
        {
            dataGridView1.DataSource = null;

            string url = "https://mhl.khl.ru" + _Players_list[index].Item2;
            string html = DownloadString(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            HtmlNodeCollection nodes_stat_headers = doc.DocumentNode.SelectNodes("//table[@class='site_table site_table-denser']/thead/tr/th");
            HtmlNodeCollection nodes_stat_data_tr = doc.DocumentNode.SelectNodes("//table[@class='site_table site_table-denser']/tr");

            // Create columns
            DataTable player_stat_table = new DataTable();
            if (nodes_stat_headers != null)
            {
                foreach (HtmlNode header in nodes_stat_headers)
                {
                    string name = header.InnerText;
                    name = name.Replace("&quote;", "");
                    name = name.Replace("/", "|");
                    name = name.Replace("+", "plus");
                    name = name.Replace("-", "minus");

                    player_stat_table.Columns.Add(name, typeof(string));
                }
            }

            // Fill Table
            if (nodes_stat_data_tr != null)
            {
                foreach (HtmlNode statRow in nodes_stat_data_tr)
                {
                    HtmlNodeCollection rowStats_arr = statRow.SelectNodes("./td");
                    DataRow row = player_stat_table.NewRow();

                    for (int i = 0; i < rowStats_arr.Count; i++)
                    {
                        row[player_stat_table.Columns[i].ColumnName] = rowStats_arr[i].InnerText.Trim();
                    }
                    player_stat_table.Rows.Add(row);
                }
            }

            dataGridView1.DataSource = player_stat_table.DefaultView;

            // Set Columns size
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                if (i == 0)
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                else
                {
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dataGridView1.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
        }
        // Get naturalstat teams stat
        public void GetNaturalstatTeamsData(string natural_equals)
        {
            dataGridView1.DataSource = null;

            string url = "https://www.naturalstattrick.com/teamtable.php" + natural_equals;
            string html = DownloadString(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);


            HtmlNodeCollection nodes_stat_headers = doc.DocumentNode.SelectNodes("//table[@id='teams']/thead/tr/th");
            HtmlNodeCollection nodes_stat_data_tr = doc.DocumentNode.SelectNodes("//table[@id='teams']/tbody/tr");

            // Create columns
            DataTable player_stat_table = new DataTable();
            if (nodes_stat_headers != null)
            {
                foreach (HtmlNode header in nodes_stat_headers)
                {
                    if (header.InnerText != "")
                    {
                        player_stat_table.Columns.Add(header.InnerText, typeof(string));
                    }
                }
            }

            //Fill Table
            if (nodes_stat_data_tr != null)
            {
                foreach (HtmlNode statRow in nodes_stat_data_tr)
                {
                    HtmlNodeCollection rowStats_arr = statRow.SelectNodes("./td");
                    DataRow row = player_stat_table.NewRow();

                    for (int i = 0; i < rowStats_arr.Count; i++)
                    {
                        if (i != 0 && i != 3)
                        {
                            string header_el = nodes_stat_headers[i].InnerText;
                            row[header_el] = rowStats_arr[i].InnerText.Trim();
                        }
                        else if(i == 3)
                        {
                            string header_el = nodes_stat_headers[i].InnerText;
                            string time_str = rowStats_arr[i].InnerText.Trim();
                            double time_double = Double.Parse(time_str, CultureInfo.InvariantCulture);
                            time_str = Math.Round(time_double, 0).ToString();
                            row[header_el] = time_str;
                        }
                        
                    }
                    player_stat_table.Rows.Add(row);
                }
            }

            dataGridView1.DataSource = player_stat_table.DefaultView;

            //Set Columns size
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                if (i != 0)
                    dataGridView1.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                
                dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }
        // Get naturalstat players by team
        public void GetNaturalstatPlayersByTeam(string team, string fromseason, string thruseason)
        {
            dataGridView1.DataSource = null;

            Tuple<string, string>[] teamsAndAbbriviatures = new Tuple<string, string>[]
            {
                new Tuple<string, string>("Anaheim Ducks", "ANA"),
                new Tuple<string, string>("Arizona Coyotes", "ARI"),
                new Tuple<string, string>("Boston Bruins", "BOS"),
                new Tuple<string, string>("Buffalo Sabres", "BUF"),
                new Tuple<string, string>("Carolina Hurricanes", "CAR"),
                new Tuple<string, string>("Columbus Blue Jackets", "CBJ"),
                new Tuple<string, string>("Calgary Flames", "CGY"),
                new Tuple<string, string>("Chicago Blackhawks", "CHI"),
                new Tuple<string, string>("Colorado Avalanche", "COL"),
                new Tuple<string, string>("Detroit Red Wings", "DET"),
                new Tuple<string, string>("Edmonton Oilers", "EDM"),
                new Tuple<string, string>("Florida Panthers", "FLA"),
                new Tuple<string, string>("Los Angeles Kings", "L.A"),
                new Tuple<string, string>("Minnesota Wild", "MIN"),
                new Tuple<string, string>("Montreal Canadiens", "MTL"),
                new Tuple<string, string>("New Jersey Devils", "N.J"),
                new Tuple<string, string>("Nashville Predators", "NSH"),
                new Tuple<string, string>("New York Islanders", "NYI"),
                new Tuple<string, string>("New York Rangers", "NYR"),
                new Tuple<string, string>("Ottawa Senators", "OTT"),
                new Tuple<string, string>("Philadelphia Flyers", "PHI"),
                new Tuple<string, string>("Pittsburgh Penguins", "PIT"),
                new Tuple<string, string>("San Jose Sharks", "S.J"),
                new Tuple<string, string>("Seattle Kraken", "SEA"),
                new Tuple<string, string>("St Louis Blues", "STL"),
                new Tuple<string, string>("Tampa Bay Lightning", "T.B"),
                new Tuple<string, string>("Toronto Maple Leafs", "TOR"),
                new Tuple<string, string>("Vancouver Canucks", "VAN"),
                new Tuple<string, string>("Vegas Golden Knights", "VGK"),
                new Tuple<string, string>("Winnipeg Jets", "WPG"),
                new Tuple<string, string>("Washington Capitals", "WSH")
            };
            string team_abbrifiature = null;
            foreach(Tuple<string, string> team_tup in teamsAndAbbriviatures)
            {
                if (team == team_tup.Item1)
                    team_abbrifiature = team_tup.Item2;
            }
            
            string html = DownloadString($"http://www.naturalstattrick.com/playerteams.php?fromseason={fromseason}&thruseason={thruseason}&stype=2&sit=all&score=all&stdoi=std&rate=n&team={team_abbrifiature}&pos=S&loc=B&toi=0&gpfilt=none&fd=&td=&tgp=410&lines=single&draftteam=ALL");
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            HtmlNodeCollection nodes_stat_headers = doc.DocumentNode.SelectNodes("//table[@id='indreg']/thead/tr/th");
            HtmlNodeCollection nodes_stat_data_tr = doc.DocumentNode.SelectNodes("//table[@id='indreg']/tbody/tr");

            // Create columns
            DataTable player_stat_table = new DataTable();
            if (nodes_stat_headers != null)
            {
                foreach (HtmlNode header in nodes_stat_headers)
                {
                    if (header.InnerText != "")
                    {
                        player_stat_table.Columns.Add(header.InnerText, typeof(string));
                    }
                }
            }

            //Fill Table
            if (nodes_stat_data_tr != null)
            {
                foreach (HtmlNode statRow in nodes_stat_data_tr)
                {
                    HtmlNodeCollection rowStats_arr = statRow.SelectNodes("./td");
                    DataRow row = player_stat_table.NewRow();

                    for (int i = 0; i < rowStats_arr.Count; i++)
                    {
                        if (i != 0 && i != 4)
                        {
                            string header_el = nodes_stat_headers[i].InnerText;
                            row[header_el] = rowStats_arr[i].InnerText.Trim();
                        }
                        else if(i == 4)
                        {
                            string header_el = nodes_stat_headers[i].InnerText;
                            string time_str = rowStats_arr[i].InnerText.Trim(); ;
                            int time = (int)Decimal.Parse(time_str, CultureInfo.InvariantCulture);
                            
                            row[header_el] = time;
                        }
                    }
                    player_stat_table.Rows.Add(row);
                }
            }

            dataGridView1.DataSource = player_stat_table.DefaultView;

            //Set Columns size
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                if (i != 0)
                    dataGridView1.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }


        }
        // Get Championat Teams
        public void GetChampionatTeams()
        {
            dataGridView1.DataSource = null;

            List<string> season = new List<string>();
            season.Add("3929");
            season.Add("4449");

            string result = null;
            if (comboBoxSeasonFrom.DropDownStyle == ComboBoxStyle.DropDown)
            {
                season.Add(comboBoxSeasonFrom.Text);
                result = DownloadString($"https://www.championat.com/hockey/_superleague/tournament/{comboBoxSeasonFrom.Text}/teams/");
            }
            else
                result = DownloadString($"https://www.championat.com/hockey/_superleague/tournament/{season[comboBoxSeasonFrom.SelectedIndex]}/teams/");

            _Clubs_list.Clear();

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(result);
            HtmlNodeCollection nodes_div = null;

            nodes_div = doc.DocumentNode.SelectNodes("//div[contains(@class, 'page-content')][2]/div/div/div[contains(@class, 'teams-item')]");
            for (int i = 0; i < nodes_div.Count(); i++)
            {
                HtmlNode node_url = nodes_div[i].SelectSingleNode("./a");
                HtmlNode node_name = nodes_div[i].SelectSingleNode("./a/div/div[1]");
                _Clubs_list.Add(new Tuple<string, string>(node_name.InnerText, node_url.GetAttributeValue("href", string.Empty).Replace("result", "tstat")));
            }

            if (result != null)
            {
                DataTable teams_table = new DataTable();
                teams_table.Columns.Add("Команда", typeof(string));
                
                if (_Clubs_list.Count() > 0)
                {
                    foreach (Tuple<string, string> name_url in _Clubs_list)
                    {
                        DataRow row = teams_table.NewRow();
                        row["Команда"] = name_url.Item1;
                        teams_table.Rows.Add(row);

                    }

                    dataGridView1.DataSource = teams_table.DefaultView;
                }
            }
        }
        // Get Championat team stat
        public void GetChampionatTeamStat(int index)
        {
            dataGridView1.DataSource = null;

            string url = "https://www.championat.com" + _Clubs_list[index].Item2;
            string html = DownloadString(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            HtmlNodeCollection nodes_stat_data_tr = doc.DocumentNode.SelectNodes("//table[@class='table table-stripe _indent-top']/tbody/tr");

            // Create columns
            DataTable player_stat_table = new DataTable();
            player_stat_table.Columns.Add("Показатель", typeof(string));
            player_stat_table.Columns.Add("Всего сум.", typeof(string));
            player_stat_table.Columns.Add("Всего сред.", typeof(string));
            player_stat_table.Columns.Add("Дома сум.", typeof(string));
            player_stat_table.Columns.Add("Дома сред.", typeof(string));
            player_stat_table.Columns.Add("В гостях сум.", typeof(string));
            player_stat_table.Columns.Add("В гостях сред.", typeof(string));

            if (nodes_stat_data_tr != null)
            {
                foreach (HtmlNode statRow in nodes_stat_data_tr)
                {
                    HtmlNodeCollection rowStats_arr = statRow.SelectNodes("./td");
                    DataRow row = player_stat_table.NewRow();

                    if(rowStats_arr.Count < 6)
                    {
                        for(int i = 0; i < 7; i++)
                        {
                            if(i == 2 || i == 4 || i == 6)
                            {
                                row[player_stat_table.Columns[i].ColumnName] = "";
                            }
                            else if(i <= 1)
                            {
                                row[player_stat_table.Columns[i].ColumnName] = rowStats_arr[i].InnerText;
                            }
                            else if(i == 3)
                            {
                                row[player_stat_table.Columns[i].ColumnName] = rowStats_arr[i - 1].InnerText;
                            }
                            else
                            {
                                row[player_stat_table.Columns[i].ColumnName] = rowStats_arr[i - 2].InnerText;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            row[player_stat_table.Columns[i].ColumnName] = rowStats_arr[i].InnerText;
                        }
                    }
                    
                    player_stat_table.Rows.Add(row);
                }
            }

            dataGridView1.DataSource = player_stat_table.DefaultView;

            //Set Columns size
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                if (i != 0)
                {
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dataGridView1.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                else
                {
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
        }
        // Get Elit Clubs and show it
        public void GetEliteClubsAndShow(string url)
        {
            dataGridView1.DataSource = null;

            _Clubs_list.Clear();

            string html = DownloadString(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            HtmlNodeCollection teams = doc.DocumentNode.SelectNodes("//div[@id='standings-and-player-stats']/div/div/div[3]/table/tbody/tr/td[@class='team']/a");

            if (teams != null)
            {
                foreach(HtmlNode team in teams)
                {
                    _Clubs_list.Add(new Tuple<string, string>(team.InnerText, team.GetAttributeValue("href", string.Empty) + "?tab=stats#players"));
                }
            }

            // Show table
            DataTable teams_table = new DataTable();
            teams_table.Columns.Add("Команда", typeof(string));

            if (_Clubs_list.Count() > 0)
            {
                foreach (Tuple<string, string> name_url in _Clubs_list)
                {
                    DataRow row = teams_table.NewRow();
                    row["Команда"] = name_url.Item1.Trim();
                    teams_table.Rows.Add(row);

                }

                dataGridView1.DataSource = teams_table.DefaultView;
            }
        }
        // Get Elit Team stats by club
        public void GetElitTeamStats(int index)
        {
            dataGridView1.DataSource = null;

            string url = _Clubs_list[index].Item2;
            string html = DownloadString(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            HtmlNodeCollection nodes_stat_headers = doc.DocumentNode.SelectNodes("(//thead)[2]/tr/th");
            HtmlNodeCollection nodes_tbody = doc.DocumentNode.SelectNodes("//table[@class='table table-striped table-sortable skater-stats highlight-stats']/tbody");
            List<HtmlNode> all_tr = new List<HtmlNode>();

            // Create columns
            DataTable player_stat_table = new DataTable();
            if (nodes_stat_headers != null)
            {
                for (int i = 1; i < nodes_stat_headers.Count; i++)
                {
                    try
                    {
                        if (nodes_stat_headers[i].InnerText == "&nbsp;")
                            player_stat_table.Columns.Add("|", typeof(string));
                        else
                            player_stat_table.Columns.Add(nodes_stat_headers[i].InnerText, typeof(string));
                    }
                    catch
                    {
                        player_stat_table.Columns.Add("p_" + nodes_stat_headers[i].InnerText, typeof(string));
                    }
                }
            }

            // Gett all tr
            if (nodes_tbody != null)
            {
                for(int i = 0; i < nodes_tbody.Count; i++)
                {
                    HtmlNodeCollection trs = nodes_tbody[i].SelectNodes("./tr");

                    foreach (HtmlNode tr in trs)
                        all_tr.Add(tr);
                }
            }

            // Fill table
            foreach (HtmlNode statRow in all_tr)
            {
                HtmlNodeCollection rowStats_arr = statRow.SelectNodes("./td");
                if (rowStats_arr[0].InnerText != "&nbsp;")
                {
                    DataRow row = player_stat_table.NewRow();

                    for (int i = 1; i < rowStats_arr.Count; i++)
                    {
                        if (rowStats_arr[i].InnerText.Trim() != "&nbsp;")
                            row[player_stat_table.Columns[i - 1].ColumnName] = rowStats_arr[i].InnerText.Trim().Replace("&nbsp;", "");
                        else
                            row[player_stat_table.Columns[i - 1].ColumnName] = "";
                    }
                    player_stat_table.Rows.Add(row);
                }
            }

            dataGridView1.DataSource = player_stat_table.DefaultView;

            // Set Columns size
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                if (i == 0)
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                else
                {
                    dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dataGridView1.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
        }
        public void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            int comboIndex = comboBox1.SelectedIndex;

            switch (comboIndex)
            {
                case 0:
                    if (dataGridView1.Columns[0].Name == "Команда")
                    {
                        dataGridView1.DataSource = null;
                        GetKHLPlayers(rowIndex);
                        FillPlayers();
                    }
                    else if (dataGridView1.Columns[0].Name == "Игрок")
                    {
                        GetKHLPlayerStatAndFill(rowIndex);

                        // formula element visible
                        textBoxFormula.Clear();
                        labelFormula.Visible = true;
                        textBoxFormula.Visible = true;
                        buttonFormulaSave.Visible = true;
                    }
                    else if(dataGridView1.Columns[0].Name == "Турнир | Команда")
                    {
                        textBoxFormula.AppendText($"[{dataGridView1.Columns[e.ColumnIndex].Name};{rowIndex}]");
                    }
                    break;
                case 1:
                    if (dataGridView1.Columns[0].Name == "Команда")
                    {
                        dataGridView1.DataSource = null;
                        GetVHLPlayers(rowIndex);
                        FillPlayers();
                    }
                    else if (dataGridView1.Columns[0].Name == "Игрок")
                    {
                        dataGridView1.DataSource = null;
                        GetVHLPlayerStatAndFill(rowIndex);

                        // formula element visible
                        textBoxFormula.Clear();
                        labelFormula.Visible = true;
                        textBoxFormula.Visible = true;
                        buttonFormulaSave.Visible = true;
                    }
                    else if (dataGridView1.Columns[0].Name == "Сезон | Клуб")
                    {
                        textBoxFormula.AppendText($"[{dataGridView1.Columns[e.ColumnIndex].Name};{rowIndex}]");
                    }
                    break;
                case 2:
                    if (dataGridView1.Columns[0].Name == "Команда")
                    {
                        dataGridView1.DataSource = null;
                        GetMHLPlayers(rowIndex);
                        FillPlayers();
                    }
                    else if (dataGridView1.Columns[0].Name == "Игрок")
                    {
                        dataGridView1.DataSource = null;
                        GetMHLPlayerStatAndFill(rowIndex);

                        // formula element visible
                        textBoxFormula.Clear();
                        labelFormula.Visible = true;
                        textBoxFormula.Visible = true;
                        buttonFormulaSave.Visible = true;
                    }
                    else if (dataGridView1.Columns[0].Name == "Турнир | Клуб")
                    {
                        textBoxFormula.AppendText($"[{dataGridView1.Columns[e.ColumnIndex].Name};{rowIndex}]");
                    }
                    break;
                case 3:
                    if (dataGridView1.Columns[e.ColumnIndex].Name == "Team")
                    {
                        string team_click = dataGridView1[e.ColumnIndex, e.RowIndex].Value.ToString();
                        string dateFrom = null;
                        string dateThru = null;
                        if (comboBoxSeasonFrom.SelectedIndex >= 0)
                        {
                            dateFrom = comboBoxSeasonFrom.Text;
                            dateFrom = dateFrom.Replace("-", "");

                            dateThru = comboBoxSeasonThru.Text;
                            dateThru = dateThru.Replace("-", "");
                        }

                        dataGridView1.DataSource = null;
                        GetNaturalstatPlayersByTeam(team_click, dateFrom, dateThru);

                        textBoxFormula.Clear();
                    }
                    else if (dataGridView1.Columns[0].Name == "Team")
                    {
                        textBoxFormula.AppendText($"[{dataGridView1.Columns[e.ColumnIndex].Name};{rowIndex}]");
                    }
                    else if (dataGridView1.Columns[0].Name == "Player")
                    {
                        textBoxFormula.AppendText($"[{dataGridView1.Columns[e.ColumnIndex].Name};{rowIndex}]");
                    }
                    break;
                case 4:
                    if (dataGridView1.Columns[0].Name == "Команда")
                    {
                        dataGridView1.DataSource = null;
                        GetChampionatTeamStat(rowIndex);

                        // formula element visible
                        textBoxFormula.Clear();
                        labelFormula.Visible = true;
                        textBoxFormula.Visible = true;
                        buttonFormulaSave.Visible = true;
                    }
                    else if (dataGridView1.Columns[0].Name == "Показатель")
                    {
                        textBoxFormula.AppendText($"[{dataGridView1.Columns[e.ColumnIndex].Name};{rowIndex}]");
                    }
                    break;
                case 5:
                    if (dataGridView1.Columns[0].Name == "Лига")
                    {
                        dataGridView1.DataSource = null;
                        string[] leagues_url_arr = new string[]
                        {
                            "https://www.eliteprospects.com/league/shl",
                            "https://www.eliteprospects.com/league/hockeyallsvenskan",
                            "https://www.eliteprospects.com/league/nl",
                            "https://www.eliteprospects.com/league/khl",
                            "https://www.eliteprospects.com/league/vhl",
                            "https://www.eliteprospects.com/league/mhl",
                            "https://www.eliteprospects.com/league/alpshl",
                            "https://www.eliteprospects.com/league/del",
                            "https://www.eliteprospects.com/league/del2",
                            "https://www.eliteprospects.com/league/czech",
                            "https://www.eliteprospects.com/league/slovakia",
                            "https://www.eliteprospects.com/league/icehl",
                            "https://www.eliteprospects.com/league/ligue-magnus",
                            "https://www.eliteprospects.com/league/liiga",
                            "https://www.eliteprospects.com/league/mestis",
                            "https://www.eliteprospects.com/league/denmark",
                            "https://www.eliteprospects.com/league/eihl",
                            "https://www.eliteprospects.com/league/poland",
                            "https://www.eliteprospects.com/league/norway",
                            "https://www.eliteprospects.com/league/erste-liga"
                        };
                        GetEliteClubsAndShow(leagues_url_arr[rowIndex]);
                    }
                    else if (dataGridView1.Columns[0].Name == "Команда")
                    {
                        dataGridView1.DataSource = null;
                        GetElitTeamStats(rowIndex);

                        // formula element visible
                        textBoxFormula.Clear();
                        labelFormula.Visible = true;
                        textBoxFormula.Visible = true;
                        buttonFormulaSave.Visible = true;
                    }
                    else if (dataGridView1.Columns[0].Name == "Player")
                    {
                        textBoxFormula.AppendText($"[{dataGridView1.Columns[e.ColumnIndex].Name};{rowIndex}]");
                    }
                    break;
            }
        }
        public void back_button_Click(object sender, EventArgs e)
        {
            //if (dataGridView1.Columns[0].Name == "Турнир / Команда")
            //{
            //    FillPlayers();
            //}
            //TableFill();

            //back_button.Enabled = false;
        }
        public void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            TableFill();

            if (comboBox1.SelectedIndex == 3)
            {
                // Seasons make visible
                labelSeasonFrom.Visible = true;
                labelSeasonThru.Visible = true;
                comboBoxSeasonFrom.Visible = true;
                comboBoxSeasonThru.Visible = true;

                comboBoxSeasonFrom.Items.Clear();
                comboBoxSeasonThru.Items.Clear();

                string[] seasons = new string[]
                {
                    "2007-2008",
                    "2008-2009",
                    "2009-2010",
                    "2010-2011",
                    "2011-2012",
                    "2013",
                    "2013-2014",
                    "2014-2015",
                    "2015-2016",
                    "2016-2017",
                    "2017-2018",
                    "2018-2019",
                    "2019-2020",
                    "2020-2021",
                    "2021-2022",
                    "2022-2023",
                    "2023-2024",
                    "2024-2025",
                    "Другой"
                };

                comboBoxSeasonFrom.Items.AddRange(seasons);
                comboBoxSeasonThru.Items.AddRange(seasons);

                buttonSeasonFind.Visible = true;
            }
            else if(comboBox1.SelectedIndex == 4)
            {
                // Seasons make visible
                labelSeasonFrom.Visible = true;
                comboBoxSeasonFrom.Visible = true;
                labelSeasonThru.Visible = false;
                comboBoxSeasonThru.Visible = false;

                comboBoxSeasonFrom.Items.Clear();
                comboBoxSeasonThru.Items.Clear();

                comboBoxSeasonFrom.Items.Add("2020/2021");
                comboBoxSeasonFrom.Items.Add("2021/2022");
                comboBoxSeasonFrom.Items.Add("2022/2023");
                comboBoxSeasonFrom.Items.Add("2023/2024");
                comboBoxSeasonFrom.Items.Add("2024/2025");
                comboBoxSeasonFrom.Items.Add("Другой");

                buttonSeasonFind.Visible = true;
            }
            else
            {
                comboBoxSeasonFrom.Items.Clear();
                comboBoxSeasonThru.Items.Clear();

                // Seasons make invisible
                labelSeasonFrom.Visible = false;
                labelSeasonThru.Visible = false;
                comboBoxSeasonFrom.Visible = false;
                comboBoxSeasonThru.Visible = false;

                buttonSeasonFind.Visible = false;
            }

            // formula element visible
            labelFormula.Visible = false;
            textBoxFormula.Visible = false;
            buttonFormulaSave.Visible = false;
            labelFormula.Visible = false;
        }
        public void buttonSeasonFind_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 3)
            {
                string fromseason = comboBoxSeasonFrom.Text;
                fromseason = fromseason.Replace("-", "");
                string thruseason = comboBoxSeasonThru.Text;
                thruseason = thruseason.Replace("-", "");

                string natural_equals = $"?fromseason={fromseason}&thruseason={thruseason}&stype=2&sit=all&score=all&rate=n&team=all&loc=B&fd=&td=";

                GetNaturalstatTeamsData(natural_equals);

                // formula element visible
                textBoxFormula.Clear();
                labelFormula.Visible = true;
                textBoxFormula.Visible = true;
                buttonFormulaSave.Visible = true;
            }
            else if(comboBox1.SelectedIndex == 4)
            {
                GetChampionatTeams();
            }
        }
        // Math textbox press Enter key
        public void textBoxMath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Expression express = new Expression(textBoxMath.Text.Remove(0, 1));
                string result = express.Evaluate().ToString();
                MessageBox.Show(result);
            }
        }
        public void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (textBoxMath.Text.Length > 3)
            {
                if (textBoxMath.Text.Contains("СУМ"))
                {
                    DataGridViewSelectedCellCollection cells_sel = dataGridView1.SelectedCells;
                    List<double> sum_list = new List<double>();

                    for (int i = 0; i < cells_sel.Count; i++)
                    {
                        string cell_text = cells_sel[i].Value.ToString();

                        if (Int32.TryParse(cell_text, out int res))
                            sum_list.Add((double)res);
                        else if (Double.TryParse(cell_text, NumberStyles.Float, CultureInfo.InvariantCulture, out double resD))
                        {
                            sum_list.Add(resD);
                        }
                        else if (cell_text.Contains('%'))
                        {
                            string cut_persent = cell_text.Remove(cell_text.IndexOf('%'));
                            if (Int32.TryParse(cut_persent, out int persent))
                            {
                                sum_list.Add((double)persent / (double)100);
                            }
                        }
                    }

                    double resultD = sum_list.Sum();
                    string result = "";

                    if (resultD % 1 == 0)
                        result = Convert.ToInt32(resultD).ToString();
                    else
                        result = resultD.ToString("0.00", CultureInfo.InvariantCulture);

                    textBoxMath.Text = textBoxMath.Text.Replace("СУМ", "");
                    textBoxMath.AppendText(result);
                }
                else if (textBoxMath.Text.Contains("СРЗНАЧ"))
                {
                    DataGridViewSelectedCellCollection cells_sel = dataGridView1.SelectedCells;
                    List<double> sum_list = new List<double>();

                    for (int i = 0; i < cells_sel.Count; i++)
                    {
                        string cell_text = cells_sel[i].Value.ToString();

                        if (Int32.TryParse(cell_text, out int res))
                            sum_list.Add((double)res);
                        else if (Double.TryParse(cell_text, NumberStyles.Float, CultureInfo.InvariantCulture, out double resD))
                        {
                            sum_list.Add(resD);
                        }
                        else if (cell_text.Contains('%'))
                        {
                            string cut_persent = cell_text.Remove(cell_text.IndexOf('%'));
                            if (Int32.TryParse(cut_persent, out int persent))
                            {
                                sum_list.Add((double)persent / (double)100);
                            }
                        }
                    }

                    double resultD = sum_list.Sum();
                    resultD = resultD / sum_list.Count;
                    string result = "";

                    if (resultD % 1 == 0)
                        result = Convert.ToInt32(resultD).ToString();
                    else
                        result = resultD.ToString("0.00", CultureInfo.InvariantCulture);

                    textBoxMath.Text = textBoxMath.Text.Replace("СРЗНАЧ", "");
                    textBoxMath.AppendText(result);
                }
                else if (textBoxMath.Text.Contains("УМНОЖ"))
                {
                    DataGridViewSelectedCellCollection cells_sel = dataGridView1.SelectedCells;
                    List<double> sum_list = new List<double>();

                    for (int i = 0; i < cells_sel.Count; i++)
                    {
                        string cell_text = cells_sel[i].Value.ToString();

                        if (Int32.TryParse(cell_text, out int res))
                            sum_list.Add((double)res);
                        else if (Double.TryParse(cell_text, NumberStyles.Float, CultureInfo.InvariantCulture, out double resD))
                        {
                            sum_list.Add(resD);
                        }
                        else if (cell_text.Contains('%'))
                        {
                            string cut_persent = cell_text.Remove(cell_text.IndexOf('%'));
                            if (Int32.TryParse(cut_persent, out int persent))
                            {
                                sum_list.Add((double)persent / (double)100);
                            }
                        }
                    }

                    double resultD = sum_list.Aggregate((a, x) => a * x);
                    string result = "";

                    if (resultD % 1 == 0)
                        result = Convert.ToInt32(resultD).ToString();
                    else
                        result = resultD.ToString("0.00", CultureInfo.InvariantCulture);

                    textBoxMath.Text = textBoxMath.Text.Replace("УМНОЖ", "");
                    textBoxMath.AppendText(result);
                }
            }


            if (dataGridView1.SelectedCells.Count > 1 && comboBox1.SelectedIndex == 5)
            {
                DataGridViewSelectedCellCollection selected_cells = dataGridView1.SelectedCells;

                textBoxFormula.AppendText("(");

                for(int i = 0; i < selected_cells.Count; i++)
                {
                    if (i != selected_cells.Count - 1)
                        textBoxFormula.AppendText($"[{dataGridView1.Columns[selected_cells[i].ColumnIndex].Name};{selected_cells[i].RowIndex}]+");
                    else
                        textBoxFormula.AppendText($"[{dataGridView1.Columns[selected_cells[i].ColumnIndex].Name};{selected_cells[i].RowIndex}]");
                }

                textBoxFormula.AppendText(")");
            }
        }
        // Formula save to txt
        public void buttonFormulaSave_Click(object sender, EventArgs e)
        {
            if(textBoxFormula.Text != "")
            {
                string name = dataGridView1.Columns[0].Name;
                name = name.Replace("|", "");
                name = name.Replace("/", "");
                name = name.Replace(" ", "");

                File.AppendAllText($"{comboBox1.Text}_{name}", textBoxFormula.Text + Environment.NewLine);

                MessageBox.Show("Формула сохранена!");

                textBoxFormula.Clear();
            }
        }
        // Change Formula textbox
        public void textBoxFormula_TextChanged(object sender, EventArgs e)
        {
            if (textBoxFormula.Text != "")
                buttonFormulaSave.Enabled = true;
            else
                buttonFormulaSave.Enabled = false;
        }
        // Comapre two teams or players
        public void buttonCompare_Click(object sender, EventArgs e)
        {
            CompareTeamsOrPlayers compare_form = new CompareTeamsOrPlayers();
            compare_form.Show();
        }
        // Season From Change
        private void comboBoxSeasonFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxSeasonFrom.SelectedIndex == comboBoxSeasonFrom.Items.Count - 1)
                comboBoxSeasonFrom.DropDownStyle = ComboBoxStyle.DropDown;
            else
                comboBoxSeasonFrom.DropDownStyle = ComboBoxStyle.DropDownList;
        }
        // Season Thru Change
        private void comboBoxSeasonThru_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxSeasonThru.SelectedIndex == comboBoxSeasonThru.Items.Count - 1)
                comboBoxSeasonThru.DropDownStyle = ComboBoxStyle.DropDown;
            else
                comboBoxSeasonThru.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://vk.com/raduzhnyi_poni") { UseShellExecute = true });
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

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://t.me/chack_frozzy") { UseShellExecute = true });
        }

        private void pictureBox2_MouseEnter(object sender, EventArgs e)
        {
            pictureBox2.Image = Resources.telegram_hover;
        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            pictureBox2.Image = Resources.telegram;
        }
    }
}
