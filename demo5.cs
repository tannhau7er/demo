using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace p2pm
{
    class requests2
    {
        private volatile bool isCancelRequested = false;
        public void CancelWork()
        {
            isCancelRequested = true;
        }
        public void ResetWork()
        {
            isCancelRequested = false;
        }


        private string HuobiFiat;
        private int HuobiBuySumm;
        private int HuobiSellCount;
        private string HuobiBuyType;
        private string HuobiSellType;

        private Dictionary<string, int> HuobiAssets;
        private Dictionary<string, int> HuobiMethods;

        public void HuobiInit(string fiat, int fiatBuySumm, int fiatSellCount, string buyType, string sellType, List<string> uncurrentMethods)
        {
            HuobiFiat = fiat;
            HuobiBuyType = buyType;
            HuobiSellType = sellType;
            HuobiBuySumm = fiatBuySumm;
            HuobiSellCount = fiatSellCount;
            HuobiMethods = new Dictionary<string, int>();
            localdata data = new localdata();
            User user = new User();
            DBUser userdata = data.GetUserData();

            HuobiAssets = new Dictionary<string, int>();

            switch (fiat)
            {
                case "RUB":
                    HuobiMethods.Add("Тинькофф", 28);
                    HuobiMethods.Add("Сбербанк", 29);
                    HuobiMethods.Add("Альфа-банк", 25);
                    HuobiMethods.Add("СБП", 69);
                    HuobiMethods.Add("ВТБ", 27);
                    HuobiMethods.Add("QIWI", 9);
                    HuobiMethods.Add("Райффайзенбанк", 36);
                    HuobiMethods.Add("Газпромбанк", 351);
                    HuobiMethods.Add("Открытие", 103);
                    HuobiMethods.Add("Карта", 1);
                    HuobiMethods.Add("Росбанк", 358);
                    HuobiMethods.Add("Payeer", 24);
                    HuobiMethods.Add("МТС Банк", 356);
                    HuobiMethods.Add("Advcash", 20);
                    HuobiMethods.Add("Почта Банк", 357);
                    HuobiMethods.Add("ЮMoney", 19);

                    HuobiAssets.Add("USDT", 2);
                    HuobiAssets.Add("BTC", 1);
                    HuobiAssets.Add("ETH", 3);
                    HuobiAssets.Add("HT", 4);
                    break;
                case "UAH":
                    HuobiMethods.Add("Monobank", 49);
                    HuobiMethods.Add("Приватбанк", 33);
                    HuobiMethods.Add("ПУМБ", 37);
                    HuobiMethods.Add("А-Банк", 149);
                    HuobiMethods.Add("Transferwise", 34);
                    HuobiMethods.Add("izibank", 499);
                    HuobiMethods.Add("Райффайзен Аваль", 155);
                    HuobiMethods.Add("Укрсиббанк", 154);
                    HuobiMethods.Add("Ощадбанк", 350);

                    HuobiAssets.Add("USDT", 2);
                    break;
            }

            if (uncurrentMethods == null)
                return;

            if (!user.GetUser(userdata.Email, userdata.Hash))
                return;

            foreach (string uncurMethod in uncurrentMethods)
            {
                try
                {
                    var item = HuobiMethods.First(x => x.Key == uncurMethod);
                    HuobiMethods.Remove(item.Key);
                }
                catch (InvalidOperationException) { }
            }
        }


        private float GetHuobiPrice(int asset, int method, int amount, string tType, bool isBuyTradeType = true)
        {
            float price = 0f;
            string tradeType = "sell";

            if (isBuyTradeType)
            {
                if (tType == "Мейкер")
                    tradeType = "buy";
            }
            else
            {
                if (tType == "Тейкер")
                    tradeType = "buy";
            }

            int currentFiat = 11;
            switch (HuobiFiat)
            {
                case "RUB":
                    currentFiat = 11;
                    break;
                case "UAH":
                    currentFiat = 45;
                    break;
            }

            var httpWebRequest = (HttpWebRequest)WebRequest.Create($"https://otc-api.bitderiv.com/v1/data/trade-market?coinId={asset}&currency={currentFiat}&tradeType={tradeType}&currPage=1&payMethod={method}&acceptOrder=0&country=&blockType=general&online=1&range=0&amount={amount}&isThumbsUp=false&isMerchant=false&isTraded=false&onlyTradable=false");
            httpWebRequest.Method = "GET";

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var json = streamReader.ReadToEnd();
                var data = (JObject)JsonConvert.DeserializeObject(json);
                string priceStr = null;
                try
                {
                    priceStr = data["data"][0]["price"].Value<string>();
                }
                catch { }
                float.TryParse(priceStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out price);
            }

            return price;
        }
        private p2pBundle GetHuobiBundle(int asset, int method)
        {
            p2pBundle bundle = new p2pBundle();
            float buyPrice = 0, sellPrice = 0;

            buyPrice = GetHuobiPrice(asset, method, HuobiBuySumm, HuobiBuyType);
            sellPrice = GetHuobiPrice(asset, method, HuobiSellCount, HuobiSellType, false);

            if (buyPrice == 0 || sellPrice == 0)
                return null;

            float assetsCount = (float)HuobiBuySumm / buyPrice;
            float fiatCount = assetsCount * sellPrice;
            fiatCount = (float)Math.Round(fiatCount, 1);
            float spred = (fiatCount - (float)HuobiBuySumm) * 100 / (float)HuobiBuySumm;
            spred = (float)Math.Round(spred, 2);
            float margin = fiatCount - (float)HuobiBuySumm;
            margin = (float)Math.Round(margin, 2);

            bundle.FiatType = HuobiFiat;
            bundle.AssetType = HuobiAssets.Where(x => x.Value == asset).Select(x => x.Key).First();
            bundle.PaymentMethod = HuobiMethods.Where(x => x.Value == method).Select(x => x.Key).First();
            bundle.BuyPrice = buyPrice;
            bundle.AssetCount = assetsCount;
            bundle.SellPrice = sellPrice;
            bundle.FiatCount = fiatCount;
            bundle.Spread = spred;
            bundle.Margin = margin;

            return bundle;
        }
        public SortList.SortableBindingList<p2pBundle> GetHuobiAllBandles()
        {
            SortList.SortableBindingList<p2pBundle> resultList = new SortList.SortableBindingList<p2pBundle>();

            if (HuobiMethods == null)
                return null;

            try
            {
                for(int i = 0; i < HuobiMethods.Count; i++)
                {
                    if (isCancelRequested)
                        break;

                    Parallel.For(0, HuobiAssets.Count, (j, state) =>
                    {
                        if (isCancelRequested)
                            state.Stop();

                        p2pBundle bundle = null;

                        bundle = GetHuobiBundle(HuobiAssets.ElementAt(j).Value, HuobiMethods.ElementAt(i).Value);

                        if (bundle != null)
                            resultList.Add(bundle);
                    });
                }
            }
            catch (OperationCanceledException) { }

            return resultList;
        }
    }
}
