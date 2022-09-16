using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

namespace Tennis
{
    class Parsing
    {
        public IWebDriver _Driver;
        public EdgeOptions _EdgeOptions;
        public EdgeDriverService _EdgeService;

        public void Init()
        {
            _EdgeOptions = new EdgeOptions();
            _EdgeOptions.AddArgument("headless");
            _EdgeOptions.AddArgument("start-maximized");
            _EdgeOptions.AddArgument("window-size=1920,1080");

            _EdgeService = EdgeDriverService.CreateDefaultService();
            _EdgeService.HideCommandPromptWindow = true;

            _Driver = new EdgeDriver(_EdgeService, _EdgeOptions);

            // Wait 60 second page load
            _Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
        }
        public void Kill()
        {
            _Driver.Close();
            _Driver.Dispose();
        }

        public List<MatchInfo> GetMatchesInfo()
        {
            try
            {
                _Driver.Navigate().GoToUrl($"https://www.tennisexplorer.com/matches/?type=all&year={DateTime.Now.ToString("yyyy")}&month={DateTime.Now.ToString("MM")}&day={DateTime.Now.ToString("dd")}");
            }
            catch { MessageBox.Show("Сайт не отвечает, превышен лимит ожидания!"); }

            // name, href
            List<MatchInfo> matchesList = new List<MatchInfo>();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(_Driver.PageSource);

            HtmlNodeCollection matchesNames = doc.DocumentNode.SelectNodes("//div[@class='box tbl lGray']/div/div/table/tbody/tr[not(contains(@class, 'head flags'))]/td[@class='t-name']/a");
            HtmlNodeCollection matchesHrefs = doc.DocumentNode.SelectNodes("//div[@class='box tbl lGray']/div/div/table/tbody/tr[not(contains(@class, 'head flags'))]/td[@rowspan='2'][not(contains(@class,'alone-icons'))]/a");

            int j = 0;
            for (int i = 0; i < matchesNames.Count - 1; i += 2)
            {
                MatchInfo matchInfo = new MatchInfo();
                matchInfo.MatchName = matchesNames[i].InnerText + " [VS] " + matchesNames[i + 1].InnerText;
                matchInfo.MatchUrl = @"https://www.tennisexplorer.com/" + matchesHrefs[j].Attributes["href"].Value;
                matchInfo.CheckMatch = false;

                matchesList.Add(matchInfo);

                j += 1;
            }

            return matchesList;
        }
        public List<PLayerLittleInfo> GetPlayersLittleInfo(List<MatchInfo> matchesInfo, int index)
        {
            List<PLayerLittleInfo> playersList = new List<PLayerLittleInfo>();
            _Driver.Navigate().GoToUrl(matchesInfo[index].MatchUrl);

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(_Driver.PageSource);

            HtmlNodeCollection playersNode = doc.DocumentNode.SelectNodes("//th[@class='plName']/a");
            string[] tourinfo = doc.DocumentNode.SelectSingleNode("//div[@id='center']/div[@class='box boxBasic lGray'][1]").InnerText.Split(',');
            
            string tournamentName = null, tournamentStage = null, coverName = null;
            switch (tourinfo.Length)
            {
                case 4:
                    tournamentName = tourinfo[1].Trim();
                    tournamentStage = tourinfo[2].Trim();
                    coverName = tourinfo[3].Trim();
                    break;
                case 5:
                    tournamentName = tourinfo[2].Trim();
                    tournamentStage = tourinfo[3].Trim();
                    coverName = tourinfo[4].Trim();
                    break;
            }

            if (playersNode.Count == 0)
                return null;

            foreach (HtmlNode player in playersNode)
            {
                PLayerLittleInfo playerLitInfo = new PLayerLittleInfo();
                playerLitInfo.PlayerName = player.InnerText.Trim();
                playerLitInfo.PlayerHref = @"https://www.tennisexplorer.com/" + player.Attributes["href"].Value;
                
                playerLitInfo.TournamentName = tournamentName;
                playerLitInfo.TournamentStage = tournamentStage;
                playerLitInfo.CoverName = coverName;
                
                playersList.Add(playerLitInfo);
            }

            if (playersList.Count == 0)
                return null;

            return playersList;
        }
        public List<PlayerFullInfo> GetPlayersFullInfo(List<PLayerLittleInfo> playersLittleInfoList)
        {
            List<PlayerFullInfo> playersFullInfo = new List<PlayerFullInfo>();

            foreach (PLayerLittleInfo playerHref in playersLittleInfoList)
            {
                PlayerFullInfo playerFullInfo = new PlayerFullInfo();

                if (playerHref == null)
                {
                    playerFullInfo = null;
                    goto pass;
                }

                _Driver.Navigate().GoToUrl(playerHref.PlayerHref);
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(_Driver.PageSource);

                playerFullInfo.PlayerName = playerHref.PlayerName;
                HtmlAgilityPack.HtmlNode ageNode = doc.DocumentNode.SelectSingleNode("//table[@class='plDetail']/tbody/tr/td[2]/div[3]");
                string ageStr = null;
                if (ageNode != null)
                {
                    ageStr = ageNode.InnerText;
                    if(ageStr.Contains("Age: "))
                    {
                        ageStr = ageStr.Replace("Age: ", "");
                    }
                    else
                    {
                        ageNode = doc.DocumentNode.SelectSingleNode("//table[@class='plDetail']/tbody/tr/td[2]/div[2]");
                        if (ageNode != null)
                            ageStr = ageNode.InnerText;

                        if(ageStr.Contains("Age: "))
                            ageStr = ageStr.Replace("Age: ", "");
                    }
                }
                playerFullInfo.PlayerAge = ageStr;

                playerFullInfo.TournamentName = playerHref.TournamentName;
                playerFullInfo.CoverName = playerHref.CoverName;
                playerFullInfo.TournamentStage = playerHref.TournamentStage;

                playerFullInfo.PlayerCountry = doc.DocumentNode.SelectSingleNode("//table[@class='plDetail']/tbody/tr/td[2]/div[1]").InnerText.Replace("Country: ", "");

                HtmlAgilityPack.HtmlNodeCollection tourPlayedThisYearNode = doc.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr[@class='head flags']");
                int tourPlayedThisYear = 0;
                if (tourPlayedThisYearNode != null)
                    tourPlayedThisYear = tourPlayedThisYearNode.Count;
                playerFullInfo.TournamentsPlayedThisYearCount = tourPlayedThisYear;

                HtmlAgilityPack.HtmlNodeCollection QFNode, SFNode, FNode;
                QFNode = doc.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr/td[@class='round'][. = 'QF']");
                SFNode = doc.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr/td[@class='round'][. = 'SF']");
                FNode = doc.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr/td[@class='round'][. = 'F']");
                int currentYearQF = 0, currentYearSF = 0, currentYearF = 0;
                if (QFNode != null)
                    currentYearQF = QFNode.Count;
                if (SFNode != null)
                    currentYearSF = SFNode.Count;
                if (FNode != null)
                    currentYearF = FNode.Count;

                HtmlNodeCollection allTrTournament = doc.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr");
                int devisCount = GetVipTournamentMatchesCount(allTrTournament, "Davis Cup");
                int australianCount = GetVipTournamentMatchesCount(allTrTournament, "Australian Open");
                int wimbledonCount = GetVipTournamentMatchesCount(allTrTournament, "Wimbledon");
                int usOpenCount = GetVipTournamentMatchesCount(allTrTournament, "US Open");
                int frenchOpenCount = GetVipTournamentMatchesCount(allTrTournament, "French Open");
                int vipTourMatchesCount = devisCount + australianCount + wimbledonCount + usOpenCount + frenchOpenCount;

                HtmlAgilityPack.HtmlNode prevTourStageNode = doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr[@class='head flags'][2]/following::tr[1]/td[@class='round']");
                string prevTourStage = null;
                if (prevTourStageNode != null)
                    prevTourStage = prevTourStageNode.InnerText.Replace("&nbsp;", "");
                playerFullInfo.PrevTournamentStage = prevTourStage;

                if (playerFullInfo.PrevTournamentStage == "F")
                    playerFullInfo.isPrevTournamentWon = true;
                else
                    playerFullInfo.isPrevTournamentWon = false;

                HtmlAgilityPack.HtmlNode prevTourpCountryNode = doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr[@class='head flags'][2]/td");
                string prevTourpCountryStr = null;
                if(prevTourpCountryNode != null)
                    prevTourpCountryStr = prevTourpCountryNode.InnerText.Replace("&nbsp;", "");
                
                playerFullInfo.PrevTournamentCountry = prevTourpCountryStr;
                playerFullInfo.LastThreeTournamedNames = GetThreeLastTourName(doc, prevTourpCountryStr);
                
                playerFullInfo.MatchesPlayedLastTwoMonth = GetMatchesTwoMonthCount(doc);

                HtmlAgilityPack.HtmlNode singNode = doc.DocumentNode.SelectSingleNode($"//div[@id='balMenu-1-data']/table/tbody/tr[contains(., '{DateTime.Now.Year}')]/td[2]");
                int singLeft = 0, singRight = 0;
                if (singNode != null)
                {
                    string singMathesString = singNode.InnerText;
                    string[] singMathesStrArr = singMathesString.Split('/');
                    singLeft = Int32.Parse(singMathesStrArr[0]);
                    singRight = Int32.Parse(singMathesStrArr[1]);
                }

                HtmlAgilityPack.HtmlNode pairNode = doc.DocumentNode.SelectSingleNode($"//div[@id='balMenu-2-data']/table/tbody/tr[contains(., '{DateTime.Now.Year}')]/td[2]");
                int pairLeft = 0, pairRight = 0;
                if (pairNode != null)
                {
                    string pairMathesString = pairNode.InnerText;
                    string[] pairMathesStrArr = pairMathesString.Split('/');
                    pairLeft = Int32.Parse(pairMathesStrArr[0]);
                    pairRight = Int32.Parse(pairMathesStrArr[1]);
                }
                singLeft = singLeft + singRight;
                pairLeft = pairLeft + pairRight;
                playerFullInfo.MathesPlayedSinglePairLastYear = singLeft + " одиночка / " + pairLeft + " пара";

                Tuple<int, int> rank = GetRank(doc);
                playerFullInfo.CurrentPlayerRaiting = rank.Item1;
                playerFullInfo.MaxPlayerRaiting = rank.Item2;

                // PREVIOUS YEAR

                _Driver.Navigate().GoToUrl(playerHref.PlayerHref + $"/?annual={DateTime.Now.Year - 1}");
                var docPrev = new HtmlAgilityPack.HtmlDocument();
                docPrev.LoadHtml(_Driver.PageSource);

                HtmlAgilityPack.HtmlNodeCollection tourPlayedPastYearNode = docPrev.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr[@class='head flags']");
                int tourPlayedPastYearCount = 0;
                if (tourPlayedPastYearNode != null)
                    tourPlayedPastYearCount = tourPlayedPastYearNode.Count;
                playerFullInfo.TournamentsPlayedTwoYearsCount = playerFullInfo.TournamentsPlayedThisYearCount + tourPlayedPastYearCount;

                HtmlAgilityPack.HtmlNodeCollection QFPrevNode, SFPrevNode, FPrevNode;
                QFPrevNode = docPrev.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr/td[@class='round'][. = 'QF']");
                SFPrevNode = docPrev.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr/td[@class='round'][. = 'SF']");
                FPrevNode = docPrev.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr/td[@class='round'][. = 'F']");
                int prevYearQF = 0, prevYearSF = 0, prevYearF = 0;
                if (QFPrevNode != null)
                    prevYearQF = QFPrevNode.Count;
                if (SFPrevNode != null)
                    prevYearSF = SFPrevNode.Count;
                if (FPrevNode != null)
                    prevYearF = FPrevNode.Count;

                playerFullInfo.QuarterTwoYearCount = currentYearQF + prevYearQF;
                playerFullInfo.HalfTwoYearCount = currentYearSF + prevYearSF;
                playerFullInfo.FinalTwoYearCount = currentYearF + prevYearF;

                HtmlNodeCollection allTrPrevTournament = docPrev.DocumentNode.SelectNodes("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr");
                int devisPrevCount = GetVipTournamentMatchesCount(allTrPrevTournament, "Davis Cup");
                int australianPrevCount = GetVipTournamentMatchesCount(allTrPrevTournament, "Australian Open");
                int wimbledonPrevCount = GetVipTournamentMatchesCount(allTrPrevTournament, "Wimbledon");
                int usOpenPrevCount = GetVipTournamentMatchesCount(allTrPrevTournament, "US Open");
                int frenchOpenPrevCount = GetVipTournamentMatchesCount(allTrPrevTournament, "French Open");
                int vipTourMatchesPrevCount = devisPrevCount + australianPrevCount + wimbledonPrevCount + usOpenPrevCount + frenchOpenPrevCount;
                playerFullInfo.VipTournamentsMatchesPlayedCount = vipTourMatchesCount + vipTourMatchesPrevCount;

                playerFullInfo.MatchesPlayedLastYearThisWeek = GetWeeklyMatchesCount(docPrev);

          pass: playersFullInfo.Add(playerFullInfo);
            }

            return playersFullInfo;
        }

        private int GetVipTournamentMatchesCount(HtmlNodeCollection allTrTournament, string tournamentName)
        {
            int startIndex = -1, stopIndex = -1, result = -1;

            if (allTrTournament == null)
                return 0;

            for(int i = 0; i < allTrTournament.Count; i++)
            {
                HtmlAgilityPack.HtmlNode rowNode = allTrTournament[i].SelectSingleNode(".//td/a");
                if (rowNode != null) 
                {
                    string str = rowNode.InnerText.Replace("&nbsp;", "");
                    if (str == tournamentName)
                    {
                        startIndex = i;
                        break;
                    }
                }
            }

            if (startIndex == -1)
                return 0;

            for(int i = startIndex + 1; i < allTrTournament.Count; i++)
            {
                if (allTrTournament[i].Attributes["class"].Value == "head flags")
                {
                    stopIndex = i - 1;
                    break;
                }
                else if (i == allTrTournament.Count - 1)
                    stopIndex = i;
            }

            result = stopIndex - startIndex;

            return result;
        }
        private int GetWeeklyMatchesCount(HtmlAgilityPack.HtmlDocument docPrev)
        {
            int count = 0;

            for (int i = DateTime.Now.Day; i > DateTime.Now.Day - 7; i--)
            {
                HtmlAgilityPack.HtmlNodeCollection node = docPrev.DocumentNode.SelectNodes($"//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr/td[@class='first time'][. = '{i}.{DateTime.Now.ToString("MM")}.']");
                if (node != null)
                {
                    count += node.Count;
                }
            }

            return count;
        }
        private int GetMatchesTwoMonthCount(HtmlAgilityPack.HtmlDocument doc)
        {
            int count = 0;

            HtmlAgilityPack.HtmlNodeCollection thisMonthNode, pastMonthNode, prePastMonthNode;
            int thisMonthCount = 0, pastMonthCount = 0, prePastMonthCount = 0;
            thisMonthNode = doc.DocumentNode.SelectNodes($"//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr[contains(., '.{DateTime.Now.ToString("MM")}.')]");
            pastMonthNode = doc.DocumentNode.SelectNodes($"//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr[contains(., '.{DateTime.Now.AddMonths(-1).ToString("MM")}.')]");
            prePastMonthNode = doc.DocumentNode.SelectNodes($"//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr[contains(., '.{DateTime.Now.AddMonths(-2).ToString("MM")}.')]");
            
            if (thisMonthNode != null)
                thisMonthCount = thisMonthNode.Count;
            if (pastMonthNode != null)
                pastMonthCount = pastMonthNode.Count;
            if (prePastMonthNode != null)
                prePastMonthCount = prePastMonthNode.Count;

            count += thisMonthCount + pastMonthCount + prePastMonthCount;

            return count;
        }
        private string GetThreeLastTourName(HtmlAgilityPack.HtmlDocument doc, string lastTourName)
        {
            HtmlAgilityPack.HtmlNode pastTourNameNode, prePastTourNameNode;
            string pastTourName = null, prePastTourName = null, result = null;
            
            if (lastTourName == null)
                return null;

            pastTourNameNode = doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr[@class='head flags'][3]/td/a");
            prePastTourNameNode = doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'matches')][not(contains(@class, 'none'))]/table/tbody/tr[@class='head flags'][4]/td/a");

            if (pastTourNameNode != null)
                pastTourName = pastTourNameNode.InnerText.Replace("&nbsp;", "");
            if(prePastTourNameNode != null)
                prePastTourName = prePastTourNameNode.InnerText.Replace("&nbsp;", "");

            result = lastTourName;

            if (pastTourName != null)
                result += $", {pastTourName}";
            if(prePastTourName != null)
                result += $", {prePastTourName}";

            return result;
        }
        private Tuple<int, int> GetRank(HtmlAgilityPack.HtmlDocument doc)
        {
            Tuple<int, int> result = new Tuple<int, int>(-1, -1);

            HtmlAgilityPack.HtmlNode rankNode = null;
            string rankStr = null, currentRankStr = null, maxRankStr = null;
            int currentRank = -1, maxRank = -1;
            for (int i = 2; i < 5; i++)
            {
                rankNode = doc.DocumentNode.SelectSingleNode($"//table[@class='plDetail']/tbody/tr/td[2]/div[{i}]");

                if (rankNode == null)
                    continue;

                rankStr = rankNode.InnerText;

                if (rankStr.Contains("singles: "))
                {
                    currentRankStr = CustomSubstring(rankStr, "singles: ", ".");
                    maxRankStr = CustomSubstring(rankStr, " / ", ".");
                    break;
                }
            }

            Int32.TryParse(currentRankStr, out currentRank);
            Int32.TryParse(maxRankStr, out maxRank);

            result = new Tuple<int, int>(currentRank, maxRank);

            return result;
        }
        private string CustomSubstring(string input, string find_word, string last_symbol)
        {
            string subst = input.Substring(input.LastIndexOf(find_word) + find_word.Length).ToString();
            string result = subst.Substring(0, subst.IndexOf(last_symbol));

            return result;
        }
    }
    public class MatchInfo
    {
        public string MatchName { get; set; }
        public string MatchUrl { get; set; }
        public bool CheckMatch { get; set; }
    }
    public class PLayerLittleInfo
    {
        public string PlayerName { get; set; }
        public string PlayerHref { get; set; }
        public string TournamentName { get; set; }
        public string TournamentStage { get; set; }
        public string CoverName { get; set; }
    }
    public class PlayerFullInfo
    {
        public string PlayerName { get; set; }
        public string PlayerAge { get; set; }
        public string TournamentName { get; set; }
        public string CoverName { get; set; }
        public string PlayerCountry { get; set; }
        public int TournamentsPlayedThisYearCount { get; set; }
        public int TournamentsPlayedTwoYearsCount { get; set; }
        public string TournamentStage { get; set; }
        public int QuarterTwoYearCount { get; set; }
        public int HalfTwoYearCount { get; set; }
        public int FinalTwoYearCount { get; set; }
        public int VipTournamentsMatchesPlayedCount { get; set; }
        public bool isPrevTournamentWon { get; set; }
        public string PrevTournamentStage { get; set; }
        public string PrevTournamentCountry { get; set; }
        public int MatchesPlayedLastYearThisWeek { get; set; }
        public int MatchesPlayedLastTwoMonth { get; set; }
        public string LastThreeTournamedNames { get; set; }
        public string MathesPlayedSinglePairLastYear { get; set; }
        public int CurrentPlayerRaiting { get; set; }
        public int MaxPlayerRaiting { get; set; }
    }
}
