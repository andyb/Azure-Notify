using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Threading;

namespace Notify
{    
    class Program
    {
        static Dictionary<string, string> deploymentTracker = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            try
            {                                
                X509Store certStore = null;
                X509Certificate2Collection certCollection = null;
                X509Certificate2 certificate = null;                
                Uri requestUri = null;


                //service to monitor
                string serviceName = "sharpcloud-test";

                //service opration
                string operation = "hostedservices" + "/" + serviceName + "?embed-detail=true";

                // The ID for the Windows Azure subscription.
                string subscriptionId = "02db5659-617a-445a-8ed3-165bf6e519bb";

                // The thumbprint for the certificate. This certificate would have been
                // previously added as a management certificate within the Windows Azure management portal.
                string thumbPrint = "ACFC0FBD3AE4EB6E242B99B82B56DF2D240E37E6";

                //The time period to recheck the service status in seconds
                long secondsToCheck = 30;
                
                certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                certStore.Open(OpenFlags.ReadOnly);
                certCollection = certStore.Certificates.Find(
                                     X509FindType.FindByThumbprint,
                                     thumbPrint,
                                     false);
                
                certStore.Close();

                if (certCollection.Count == 0)
                {
                    throw new Exception("No certificate found containing thumbprint " + thumbPrint);
                }

                certificate = certCollection[0];
                Console.WriteLine(">> Using certificate with thumbprint: " + thumbPrint);
                Console.WriteLine(">> Fetching status of service: " + serviceName);
                Console.WriteLine(">> With subscription id of: " + subscriptionId);
                Console.WriteLine(">> Checking every " + secondsToCheck + " seconds");

                requestUri = new Uri("https://management.core.windows.net/"
                                     + subscriptionId
                                     + "/services/"
                                     + operation);


                Timer timer = new Timer((o) =>
                {                    
                    GetStatus(certificate, requestUri);
                },null,0,secondsToCheck * 1000);
                
                
                
            }
            catch (Exception e)
            {

                Console.WriteLine("Error encountered: " + e.Message);

            }
           
            Console.ReadLine();
        }

        private static void GetStatus(X509Certificate2 certificate, Uri requestUri)
        {                        
            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            httpWebRequest.ClientCertificates.Add(certificate);
            httpWebRequest.Headers.Add("x-ms-version", "2011-02-25");
            using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())            
            using (Stream responseStream = httpWebResponse.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                var result = XDocument.Parse(reader.ReadToEnd());

                var query = from e in result.Elements().Elements()
                            where e.Name == "{http://schemas.microsoft.com/windowsazure}Deployments"
                            select e;

                var list = query.ToList();

                list.ForEach(x =>
                {
                    var deployments = from e in x.Elements()
                                      select new Tuple<string, string>((string)e.Element("{http://schemas.microsoft.com/windowsazure}Url"), (string)e.Element("{http://schemas.microsoft.com/windowsazure}Status"));


                    deployments.ToList().ForEach(xx =>
                    {
                        if (deploymentTracker.ContainsKey(xx.Item1))
                        {
                            if (deploymentTracker[xx.Item1] != xx.Item2)
                            {
                                Console.WriteLine(string.Format("Status has changed for {0} to {1}"));
                                deploymentTracker[xx.Item1] = xx.Item2;
                            }
                            else
                            {
                                Console.Write(".");
                            }
                        }
                        else
                        {
                            deploymentTracker.Add(xx.Item1, xx.Item2);
                            Console.WriteLine(string.Format("Tracking {0} with a status of {1}.", xx.Item1, xx.Item2));
                        }
                    }
                    );

                }
                );
            }               
        }
    }
}
