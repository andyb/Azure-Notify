A simple console app to monitor the status of a Microsoft Azure Service and notify by email when a change occurs.
To use complete the config section in Program.cs and build in visual studio 2010. You can then run the console app exe.

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