using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PayPal
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class PayPalService : IPayPalService, IDisposable
    {
        public event Action<string, string, string, bool, int> NewPayment;

        WebServiceHost serviceHost;

        public void Dispose()
        {
            serviceHost.Abort();
        }

        public void Start()
        {
            serviceHost = new WebServiceHost(this);
            serviceHost.Open();
        }

        public void Notify(string operationId, string login, string comment, string isPublic, string gross, string currency)
        {
            try
            {
                if (NewPayment != null)
                {
                    //TODO разные валюты перевести
                    var power = (int)float.Parse(gross, new CultureInfo("en-en"));
                    NewPayment(operationId, login, comment, isPublic == "Yes" ? true : false, power);
                }
            }
            catch
            {
            }
        }
    }
}
