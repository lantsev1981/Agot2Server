using MyLibrary;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Yandex
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class YandexService : IYandexService, IDisposable
    {
        public event Action<string[], int, DateTimeOffset, string> NewPayment;

        private WebServiceHost serviceHost;

        public void Dispose()
        {
            serviceHost.Abort();
        }

        public void Start()
        {
            serviceHost = new WebServiceHost(this);
            serviceHost.Open();
        }

        public void Notify(Stream data)
        {
            try
            {
                StreamReader reader = new StreamReader(data, Encoding.UTF8);
                string str = reader.ReadToEnd();
                Parse(str);
            }
            catch
            {
            }
        }

        private void Parse(string str)
        {
            str = WebUtility.UrlDecode(str);
            NameValueCollection nq = new NameValueCollection();
            string[] strData = str.Split(new char[] { '&', '=' }, StringSplitOptions.None);
            for (int i = 0; i < strData.Length; i += 2)
                nq.Add(strData[i], strData[i + 1]);

            //проверка подлинности сообщения
            string sha1Str = string.Format("{0}&{1}&{2}&{3}&{4}&{5}&{6}&{7}&{8}",
                nq["notification_type"],
                nq["operation_id"],
                nq["amount"],
                nq["currency"],
                nq["datetime"],
                nq["sender"],
                nq["codepro"],
                "IWSgBWB4QFF8SytE5BtTLzmD",
                nq["label"]);

#if !DEBUG
            sha1Str = Crypto.SHA1Hex(sha1Str);
            if (sha1Str != nq["sha1_hash"])
                return;
#endif

            //перевод защищен кодом протекции.
            if (nq["codepro"] != "false")
                return;

            //login не известен
            if (string.IsNullOrWhiteSpace(nq["label"]))
                return;

            string[] split = nq["label"].Split('|');

            if (split.Length != 3)
                split = new string[3] { "17a87d89-b8d7-4274-9049-78d7b6af94af", "0", string.Format("Строка не распознана: {0}", nq["label"]) };

            //Сумма, которая списана со счета отправителя.
            string amount = nq["withdraw_amount"];
            if (string.IsNullOrEmpty(amount))
                amount = nq["amount"];//Сумма операции.
            if (string.IsNullOrEmpty(amount))
                return;

            int payment = (int)float.Parse(amount, new CultureInfo("en-US"));
            DateTimeOffset dT = DateTimeOffset.UtcNow;

            NewPayment?.Invoke(split, payment, dT, nq["operation_id"]);
        }
    }
}
