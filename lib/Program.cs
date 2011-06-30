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
                // X.509 certificate variables.
                X509Store certStore = null;
                X509Certificate2Collection certCollection = null;
                X509Certificate2 certificate = null;


                // URI variable.
                Uri requestUri = null;


                //service to monitor
                string serviceName = "sharpcloud-test";

                string operation = "hostedservices" + "/" + serviceName + "?embed-detail=true";

                // The ID for the Windows Azure subscription.
                string subscriptionId = "02db5659-617a-445a-8ed3-165bf6e519bb";

                // The thumbprint for the certificate. This certificate would have been
                // previously added as a management certificate within the Windows Azure management portal.
                string thumbPrint = "ACFC0FBD3AE4EB6E242B99B82B56DF2D240E37E6";

                // Open the certificate store for the current user.
                certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                certStore.Open(OpenFlags.ReadOnly);

                // Find the certificate with the specified thumbprint.
                certCollection = certStore.Certificates.Find(
                                     X509FindType.FindByThumbprint,
                                     thumbPrint,
                                     false);

                // Close the certificate store.
                certStore.Close();

                // Check to see if a matching certificate was found.
                if (certCollection.Count == 0)
                {
                    throw new Exception("No certificate found containing thumbprint " + thumbPrint);
                }

                // A matching certificate was found.
                certificate = certCollection[0];
                Console.WriteLine(">> Using certificate with thumbprint: " + thumbPrint);
                Console.WriteLine(">> Fetching status of service: " + serviceName);
                // Create the request.
                requestUri = new Uri("https://management.core.windows.net/"
                                     + subscriptionId
                                     + "/services/"
                                     + operation);


                Timer timer = new Timer((o) =>
                {
                    Console.Write("Checking...");
                    GetStatus(certificate, requestUri);
                },null,0,30000);
                
                
                
            }
            catch (Exception e)
            {

                Console.WriteLine("Error encountered: " + e.Message);

            }
           
            Console.ReadLine();
        }

        private static void GetStatus(X509Certificate2 certificate, Uri requestUri)
        {

            // Request and response variables.
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebResponse = null;

            // Stream variables.
            Stream responseStream = null;
            StreamReader reader = null;

            httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(requestUri);

            // Add the certificate to the request.
            httpWebRequest.ClientCertificates.Add(certificate);

            // Specify the version information in the header.
            httpWebRequest.Headers.Add("x-ms-version", "2011-02-25");

            // Make the call using the web request.
            httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            // Parse the web response.
            responseStream = httpWebResponse.GetResponseStream();
            reader = new StreamReader(responseStream);

            var result = XDocument.Parse(reader.ReadToEnd());

            var query = from e in result.Elements().Elements()
                        where e.Name == "{http://schemas.microsoft.com/windowsazure}Deployments"
                        select e;

            var list = query.ToList();

            list.ForEach(x =>
            {
                var deployments = from e in x.Elements()
                                  select new Tuple<string,string>( (string)e.Element("{http://schemas.microsoft.com/windowsazure}Url") , (string)e.Element("{http://schemas.microsoft.com/windowsazure}Status") );                
                
              
                deployments.ToList().ForEach(xx =>
                {
                    if(deploymentTracker.ContainsKey(xx.Item1))
                    {
                        if (deploymentTracker[xx.Item1] != xx.Item2)
                        {
                            Console.WriteLine(string.Format("Status has changed for {0} to {1}"));
                            deploymentTracker[xx.Item1] = xx.Item2;
                        }
                        else
                        {
                            Console.Write("...no status changes...");
                        }
                    }
                    else
                    {
                        deploymentTracker.Add(xx.Item1,xx.Item2);
                        Console.WriteLine(string.Format("Tracking {0} with a status of {1}.",xx.Item1,xx.Item2));
                    }                    
                }
                );

            }
            );

            
            // Close the resources no longer needed.
            httpWebResponse.Close();
            responseStream.Close();
            reader.Close();
        }
    }
}
