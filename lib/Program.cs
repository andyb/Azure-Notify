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
using System.Net.Mail;

namespace Notify
{    
    class Program
    {        
        /// <summary>
        /// Config settings - please configure the strings below to setup smtp account for email and Windows Azure service to monitor
        /// </summary>
        ///smtp host
        private static string smtpHost = "##mail.smtp.com##";
        //Username for smtp host
        private static string username = "##smtpusername##";
        //Password for smtp host
        private static string pass = "##passwordd##";
        //from email address
        private static string fromemail = "##from@you.com##";
        //to email address
        private static string toAddress = "##to@you.com##";
        //service to monitor
        private static string serviceName = "yourazuretest";
        // The ID for the Windows Azure subscription.
        private static string subscriptionId = "00000000-0000-0000-0000-000000000000";
        // The thumbprint for the certificate. This certificate would have been
        // previously added as a management certificate within the Windows Azure management portal.
        private static string thumbPrint = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        //The time period to recheck the service status in seconds
        private static long secondsToCheck = 30;        

        static void Main(string[] args)
        {
            try
            {
                X509Store certStore = null;
                X509Certificate2Collection certCollection = null;
                X509Certificate2 certificate = null;
                Uri requestUri = null;

                //service opration
                string operation = "hostedservices" + "/" + serviceName + "?embed-detail=true";

             

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
                Console.WriteLine(">> Checking every " + secondsToCheck + " seconds...starting check");

                requestUri = new Uri("https://management.core.windows.net/"
                                     + subscriptionId
                                     + "/services/"
                                     + operation);


                timer = new Timer((o) =>
                {
                    GetStatus(certificate, requestUri);
                }, null, 0, secondsToCheck * 1000);



            }
            catch (Exception ex)
            {

                Console.WriteLine("Error encountered: " + ex.Message);                
            }
            finally
            {
                Console.ReadLine();
            }
           
            
        }

        private static void GetStatus(X509Certificate2 certificate, Uri requestUri)
        {
            try
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
                                          select new Tuple<string, string>(DecodeFrom64((string)e.Element("{http://schemas.microsoft.com/windowsazure}Label")), (string)e.Element("{http://schemas.microsoft.com/windowsazure}Status"));


                        deployments.ToList().ForEach(xx =>
                        {
                            if (deploymentTracker.ContainsKey(xx.Item1))
                            {
                                if (deploymentTracker[xx.Item1] != xx.Item2)
                                {
                                    var statusChange = string.Format("Status has changed for {0} from {1} to {2}", xx.Item1, deploymentTracker[xx.Item1], xx.Item2);
                                    Console.WriteLine(statusChange + ", sending email...");
                                    deploymentTracker[xx.Item1] = xx.Item2;
                                    SendEmail(statusChange);
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
            catch (Exception ex)
            {

                Console.WriteLine("Error encountered: " + ex.Message);
            }
                  
        }

        static public string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
            return System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);            
        }


        private static void SendEmail(string statusChange)
        {
            try
            {
                using (SmtpClient client = new SmtpClient(smtpHost))
                {
                    client.Port = 2525;
                    client.Credentials = new NetworkCredential(username, pass);
                    MailAddress from = new MailAddress(fromemail, fromemail,
                                                       System.Text.Encoding.UTF8);
                    MailAddress to = new MailAddress(toAddress);
                    using (MailMessage message = new MailMessage(from, to))
                    {
                        message.Subject = statusChange;
                        message.Body = "";
                        client.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error encountered: " + ex.Message);
            }
            
        }

        //tracks deployments
        private static Dictionary<string, string> deploymentTracker = new Dictionary<string, string>();
        private static Timer timer;
    }
}
