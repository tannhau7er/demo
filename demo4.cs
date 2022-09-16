using System;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Concurrent;

namespace p2pm
{
    class requests
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


        private string BinanceFiat;
        private int BinanceBuySumm;
        private int BinanceSellCount;
        private string BinanceBuyType;
        private string BinanceSellType;
        private int SpreadStart, SpreadEnd;

        private Dictionary<string, string> BinanceMethods;
        private List<string> BinanceAssets;
        private List<string> BinanceSpotPairs = new List<string>() 
        {
            "BTC/USDT",
            "BUSD/USDT",
            "BNB/USDT",
            "ETH/USDT",
            "SHIB/USDT",
            "BNB/BTC",
            "ETH/BTC",
            "BTC/BUSD",
            "BNB/BUSD",
            "ETH/BUSD",
            "SHIB/BUSD",
            "BNB/ETH"
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fiat">Фиат</param>
        /// <param name="fiatBuySumm">Вся сумма закупки</param>
        /// <param name="fiatSellCount">Минимальная сумма каждой продажи</param>
        /// <param name="buyType">Т - тейкер, М - мейкер</param>
        /// <param name="sellType">Т - тейкер, М - мейкер</param>
        /// <param name="currentMethods">Массив методов НЕ входящих в поиск</param>
        public void BinanceInit(string fiat, int fiatBuySumm, int fiatSellCount, string buyType, string sellType, List<string> uncurrentMethods, int spreadStart, int spreadEnd)
        {
            BinanceFiat = fiat;
            BinanceAssets = new List<string>() { "USDT", "BTC", "BUSD", "BNB", "ETH", "SHIB" };
            BinanceAssets.Add(fiat);
            BinanceBuyType = buyType;
            BinanceSellType = sellType;
            BinanceBuySumm = fiatBuySumm;
            BinanceSellCount = fiatSellCount;
            BinanceMethods = new Dictionary<string, string>();
            SpreadStart = spreadStart;
            SpreadEnd = spreadEnd;
            localdata data = new localdata();
            User user = new User();
            DBUser userdata = data.GetUserData();

            // add methods to dictionary
            switch (fiat)
            {
                case "RUB":
                    BinanceMethods.Add("Tinkoff", "Тинькофф");
                    BinanceMethods.Add("RosBank", "Росбанк");
                    BinanceMethods.Add("RaiffeisenBankRussia", "Райффайзенбанк");
                    BinanceMethods.Add("QIWI", "QIWI");
                    BinanceMethods.Add("YandexMoneyNew", "ЮMoney");
                    BinanceMethods.Add("RUBfiatbalance", "Фиатный баланс");
                    BinanceMethods.Add("PostBankRussia", "Почта Банк");
                    BinanceMethods.Add("ABank", "А-Банк");
                    BinanceMethods.Add("HomeCreditBank", "Хоум Кредит");
                    BinanceMethods.Add("MTSBank", "МТС Банк");
                    BinanceMethods.Add("Payeer", "Payeer");
                    BinanceMethods.Add("Advcash", "Advcash");
                    BinanceMethods.Add("UralsibBank", "Уралсиб");
                    BinanceMethods.Add("Mobiletopup", "Счет мобильного");
                    BinanceMethods.Add("AkBarsBank", "Ак Барс");
                    BinanceMethods.Add("RussianStandardBank", "Русский стандарт");
                    BinanceMethods.Add("BankSaintPetersburg", "Банк Санкт-Петербург");
                    BinanceMethods.Add("RenaissanceCredit", "Ренессанс Кредит");
                    BinanceMethods.Add("BCSBank", "БКС");
                    break;
                case "UAH":
                    BinanceMethods.Add("Monobank", "Monobank");
                    BinanceMethods.Add("PrivatBank", "Приватбанк");
                    BinanceMethods.Add("PUMBBank", "ПУМБ");
                    BinanceMethods.Add("ABank", "А-Банк");
                    BinanceMethods.Add("Sportbank", "Sportbank");
                    BinanceMethods.Add("izibank", "izibank");
                    BinanceMethods.Add("RaiffeisenBankAval", "Райффайзен Аваль");
                    BinanceMethods.Add("Oschadbank", "Ощадбанк");
                    BinanceMethods.Add("Wise", "Wise");
                    BinanceMethods.Add("Ukrsibbank", "Укрсиббанк");
                    BinanceMethods.Add("NEO", "NEO");
                    BinanceMethods.Add("UAHfiatbalance", "Фиатный баланс");
                    BinanceMethods.Add("OTPBank", "ОТП");
                    BinanceMethods.Add("RaiffeisenBankRussia", "Райффайзенбанк");
                    BinanceMethods.Add("CreditAgricole", "Credit Agricole");
                    BinanceMethods.Add("OTPBankRussia", "ОТП Россия");
                    BinanceMethods.Add("KredoBank", "Кредобанк");
                    BinanceMethods.Add("Advcash", "Advcash");
                    BinanceMethods.Add("GEOPay", "GEO Pay");
                    break;
            }

            if (uncurrentMethods == null)
                return;

            // remove uncorrect methods from dictionary
            if (!user.GetUser(userdata.Email, userdata.Hash))
                return;

            foreach (string uncurMethod in uncurrentMethods)
            {
                try
                {
                    var item = BinanceMethods.First(x => x.Value == uncurMethod);
                    BinanceMethods.Remove(item.Key);
                }
                catch (InvalidOperationException) { }
            }
        }

        
        private float GetBinancePrice(string asset, string method, string amount, string tType, bool isBuyTradeType = true)
        {
            float price = 0f;
            string tradeType = "BUY";

            // buy summ
            if (isBuyTradeType)
            {
                if (tType == "Мейкер")
                    tradeType = "SELL";
            }
            // min sale summ
            else
            {
                if (tType == "Тейкер")
                    tradeType = "SELL";
            }

            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://p2p.binance.com/bapi/c2c/v2/friendly/c2c/adv/search");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            BinanceRequestObj jsonReqObj = new BinanceRequestObj();
            jsonReqObj.asset = asset;
            jsonReqObj.fiat = BinanceFiat;
            jsonReqObj.page = 1;
            jsonReqObj.payTypes = new string[] { method };
            jsonReqObj.rows = 2;
            jsonReqObj.transAmount = amount;
            jsonReqObj.tradeType = tradeType;

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(jsonReqObj);
                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var json = streamReader.ReadToEnd();
                var data = (JObject)JsonConvert.DeserializeObject(json);
                string priceStr = null;
                try
                {
                    priceStr = data["data"][0]["adv"]["price"].Value<string>();
                }
                catch { }
                float.TryParse(priceStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out price);
            }

            return price;
        }
        private p2pBundle GetBinanceBundle(string asset, string method, string methodName)
        {
            p2pBundle bundle = new p2pBundle();
            float buyPrice = 0, sellPrice = 0;

            buyPrice = GetBinancePrice(asset, method, BinanceBuySumm.ToString(), BinanceBuyType);
            sellPrice = GetBinancePrice(asset, method, BinanceSellCount.ToString(), BinanceSellType, false);

            if (buyPrice == 0 || sellPrice == 0)
                return null;

            float assetsCount = (float)BinanceBuySumm / buyPrice;
            float fiatCount = assetsCount * sellPrice;
            fiatCount = (float)Math.Round(fiatCount, 1);
            float spred = (fiatCount - (float)BinanceBuySumm) * 100 / (float)BinanceBuySumm;
            spred = (float)Math.Round(spred, 2);
            float margin = fiatCount - (float)BinanceBuySumm;
            margin = (float)Math.Round(margin, 2);

            bundle.FiatType = BinanceFiat;
            bundle.AssetType = asset;
            bundle.PaymentMethod = methodName;
            bundle.BuyPrice = buyPrice;
            bundle.AssetCount = assetsCount;
            bundle.SellPrice = sellPrice;
            bundle.FiatCount = fiatCount;
            bundle.Spread = spred;
            bundle.Margin = margin;

            return bundle;
        }
        public SortList.SortableBindingList<p2pBundle> GetBinanceAllBandles()
        {
            SortList.SortableBindingList<p2pBundle> resultList = new SortList.SortableBindingList<p2pBundle>();

            if (BinanceMethods == null)
                return null;

            try
            {
                Parallel.For(0, BinanceMethods.Count, (i, state) =>
                {
                    if(isCancelRequested)
                        state.Stop();

                    Parallel.For(0, BinanceAssets.Count, (j, state2) =>
                    {
                        if(isCancelRequested)
                            state2.Stop();

                        p2pBundle bundle = null;

                        bundle = GetBinanceBundle(BinanceAssets[j], BinanceMethods.ElementAt(i).Key, BinanceMethods.ElementAt(i).Value);

                        if (bundle != null)
                            resultList.Add(bundle);
                    });
                });
            }
            catch (OperationCanceledException) { }

            return resultList;
        }
        

        public SortList.SortableBindingList<p2pAssetBundle> GetAllAssetBundles(SortList.SortableBindingList<p2pBundle> bundlesList)
        {
            SortList.SortableBindingList<p2pAssetBundle> resultList = new SortList.SortableBindingList<p2pAssetBundle>();

            foreach(p2pBundle bundleX in bundlesList)
            {
                if(isCancelRequested)
                    break;

                if (bundleX == null)
                    continue;

                foreach (p2pBundle bundleY in bundlesList)
                {
                    if(isCancelRequested)
                        break;

                    if (bundleY == null)
                        continue;

                    try
                    {
                        if (bundleY.AssetType != bundleX.AssetType || bundleY.PaymentMethod == bundleX.PaymentMethod)
                            continue;
                    }
                    catch (System.NullReferenceException) { continue; }

                    p2pAssetBundle bundleResult = new p2pAssetBundle();
                    bundleResult.FiatType = bundleX.FiatType;
                    bundleResult.AssetType = bundleX.AssetType;
                    bundleResult.BuyMethod = bundleX.PaymentMethod;
                    float buyPrice = bundleX.BuyPrice;
                    bundleResult.BuyPrice = buyPrice;
                    float assetCount = BinanceBuySumm / buyPrice;
                    bundleResult.AssetCount = assetCount;
                    bundleResult.SellMethod = bundleY.PaymentMethod;
                    float sellPrice = bundleY.SellPrice;
                    bundleResult.SellPrice = sellPrice;
                    float fiatCount = assetCount * sellPrice;
                    fiatCount = (float)Math.Round(fiatCount, 1);
                    bundleResult.FiatCount = fiatCount;
                    float spread = (fiatCount - BinanceBuySumm) * 100 / BinanceBuySumm;
                    spread = (float)Math.Round(spread, 2);
                    bundleResult.Spread = spread;
                    float margin = fiatCount - BinanceBuySumm;
                    margin = (float)Math.Round(margin, 1);
                    bundleResult.Margin = margin;

                    resultList.Add(bundleResult);
                }
            }

            return resultList;
        }


        private ConcurrentDictionary<string, float> GetBinanceAllExchangeRate()
        {
            ConcurrentDictionary<string, float> result = new ConcurrentDictionary<string, float>();

            Parallel.ForEach(BinanceSpotPairs, pairSlash =>
            {
                float price = 0f;
                string pair = pairSlash.Replace("/", "");

                var httpWebRequest = (HttpWebRequest)WebRequest.Create($"https://api.binance.com/api/v3/ticker/price?symbol={pair}");
                httpWebRequest.ContentType = "text/html";
                httpWebRequest.Method = "GET";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var json = streamReader.ReadToEnd();
                    var data = (JObject)JsonConvert.DeserializeObject(json);
                    string priceStr = null;

                    priceStr = data["price"].Value<string>();
                    
                    float.TryParse(priceStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out price);
                }

                result.TryAdd(pairSlash, price);
            });

            return result;
        }
        public SortList.SortableBindingList<p2pBinanceSpotBundle> GetBinanceAllSpotBundles(SortList.SortableBindingList<p2pBundle> bundlesList)
        {
            SortList.SortableBindingList<p2pBinanceSpotBundle> resultList = new SortList.SortableBindingList<p2pBinanceSpotBundle>();
            SortList.SortableBindingList<p2pBundle> buyBundleList;
            SortList.SortableBindingList<p2pBundle> sellBundleList;
            p2pBinanceSpotBundle spotBundle;
            ConcurrentDictionary<string, float> exchangeList = GetBinanceAllExchangeRate();
            string[] pairArr;
            float price, assetSellCount, fiatCount, spread, margin;

            if (exchangeList == null)
                return null;

            foreach (KeyValuePair<string, float> exchange in exchangeList)
            {
                if (isCancelRequested)
                    break;

                buyBundleList = new SortList.SortableBindingList<p2pBundle>();
                sellBundleList = new SortList.SortableBindingList<p2pBundle>();
                pairArr = exchange.Key.Split('/');

                // split bundles on buy and sell
                foreach (p2pBundle bundle in bundlesList)
                {
                    if (bundle == null)
                        continue;

                    if (bundle.AssetType == pairArr[1])
                        buyBundleList.Add(bundle);
                    else if (bundle.AssetType == pairArr[0])
                        sellBundleList.Add(bundle);
                }

                if (buyBundleList.Count <= 0 || sellBundleList.Count <= 0)
                    continue;

                foreach(p2pBundle buyBundle in buyBundleList)
                {
                    if (isCancelRequested)
                        break;

                    foreach (p2pBundle sellBundle in sellBundleList)
                    {
                        if (isCancelRequested)
                            break;

                        spotBundle = new p2pBinanceSpotBundle();

                        price = exchange.Value;
                        assetSellCount = 1 / price * buyBundle.AssetCount;
                        fiatCount = assetSellCount * sellBundle.SellPrice;
                        spread = (fiatCount - BinanceBuySumm) * 100 / BinanceBuySumm;
                        spread = (float)Math.Round(spread, 2);
                        margin = fiatCount - BinanceBuySumm;
                        margin = (float)Math.Round(margin, 1);

                        spotBundle.FiatType = BinanceFiat;
                        spotBundle.AssetBuyType = buyBundle.AssetType;
                        spotBundle.BuyPaymentMethod = buyBundle.PaymentMethod;
                        spotBundle.BuyPrice = buyBundle.BuyPrice;
                        spotBundle.AssetBuyCount = buyBundle.AssetCount;

                        spotBundle.AssetSellType = sellBundle.AssetType;
                        spotBundle.ExchangeRange = price;
                        spotBundle.AssetSellCount = assetSellCount;
                        spotBundle.SellPaymentMethod = sellBundle.PaymentMethod;
                        spotBundle.SellPrice = sellBundle.SellPrice;
                        spotBundle.FiatCount = fiatCount;
                        spotBundle.Spread = spread;
                        spotBundle.Margin = margin;

                        resultList.Add(spotBundle);

                        //reverse
                        spotBundle = new p2pBinanceSpotBundle();

                        assetSellCount = price * sellBundle.AssetCount;
                        fiatCount = assetSellCount * buyBundle.SellPrice;
                        spread = (fiatCount - BinanceBuySumm) * 100 / BinanceBuySumm;
                        spread = (float)Math.Round(spread, 2);
                        margin = fiatCount - BinanceBuySumm;
                        margin = (float)Math.Round(margin, 1);

                        spotBundle.FiatType = BinanceFiat;
                        spotBundle.AssetBuyType = sellBundle.AssetType;
                        spotBundle.BuyPaymentMethod = sellBundle.PaymentMethod;
                        spotBundle.BuyPrice = sellBundle.BuyPrice;
                        spotBundle.AssetBuyCount = sellBundle.AssetCount;

                        spotBundle.AssetSellType = buyBundle.AssetType;
                        spotBundle.ExchangeRange = price;
                        spotBundle.AssetSellCount = assetSellCount;
                        spotBundle.SellPaymentMethod = buyBundle.PaymentMethod;
                        spotBundle.SellPrice = buyBundle.SellPrice;
                        spotBundle.FiatCount = fiatCount;
                        spotBundle.Spread = spread;
                        spotBundle.Margin = margin;

                        resultList.Add(spotBundle);
                    }
                }
            }

            return resultList;
        }
    }

    internal class BinanceRequestObj
    {
        public string asset { get; set; }
        public string fiat { get; set; }
        public int page { get; set; }
        public string[] payTypes { get; set; }
        public string publisherType { get; } = null;
        public int rows { get; set; }
        public string tradeType { get; set; }
        public string transAmount { get; set; }
    }
    public class p2pBundle
    {
        [DisplayName("Фиат")]
        public string FiatType { get; set; }
        
        [DisplayName("Крипта")]
        public string AssetType { get; set; }
        
        [DisplayName("Метод покупки")]
        public string PaymentMethod { get; set; }

        [DisplayName("Цена покупки")]
        public float BuyPrice { get; set; }

        [DisplayName("Кол-во крипты")]
        public float AssetCount { get; set; }

        [DisplayName("Цена продажи")]
        public float SellPrice { get; set; }

        [DisplayName("Кол-во фиата")]
        public float FiatCount { get; set; }

        [DisplayName("Спред %")]
        public float Spread { get; set; }

        [DisplayName("Маржа")]
        public float Margin { get; set; }
    }
    public class p2pAssetBundle
    {
        [DisplayName("Фиат")]
        public string FiatType { get; set; }
        
        [DisplayName("Крипта")]
        public string AssetType { get; set; }

        [DisplayName("Метод покупки")]
        public string BuyMethod { get; set; }

        [DisplayName("Цена покупки")]
        public float BuyPrice { get; set; }

        [DisplayName("Кол-во крипты")]
        public float AssetCount { get; set; }

        [DisplayName("Метод продажи")]
        public string SellMethod { get; set; }

        [DisplayName("Цена продажи")]
        public float SellPrice { get; set; }

        [DisplayName("Кол-во фиата")]
        public float FiatCount { get; set; }

        [DisplayName("Спред %")]
        public float Spread { get; set; }

        [DisplayName("Маржа")]
        public float Margin { get; set; }
    }
    public class p2pBinanceSpotBundle
    {
        [DisplayName("Фиат")]
        public string FiatType { get; set; }

        [DisplayName("Крипта покупки")]
        public string AssetBuyType { get; set; }

        [DisplayName("Метод покупки")]
        public string BuyPaymentMethod { get; set; }

        [DisplayName("Цена покупки")]
        public float BuyPrice { get; set; }

        [DisplayName("Кол-во крипты X")]
        public float AssetBuyCount { get; set; }

        [DisplayName("Крипта обмена")]
        public string AssetSellType { get; set; }

        [DisplayName("Курс обмена")]
        public float ExchangeRange { get; set; }

        [DisplayName("Колво крипты Y")]
        public float AssetSellCount { get; set; }

        [DisplayName("Метод продажи")]
        public string SellPaymentMethod { get; set; }

        [DisplayName("Цена продажи")]
        public float SellPrice { get; set; }

        [DisplayName("Кол-во фиата")]
        public float FiatCount { get; set; }

        [DisplayName("Спред %")]
        public float Spread { get; set; }

        [DisplayName("Маржа")]
        public float Margin { get; set; }
    }
}
