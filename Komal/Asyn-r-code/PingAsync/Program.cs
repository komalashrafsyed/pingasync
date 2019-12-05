using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System.IO;
using System.Threading;
using System.Net;
using System.Diagnostics;
using System.Data;
using System.Data.OleDb;
using System.Data.Odbc;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using System.ComponentModel;
using OfficeOpenXml;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Microsoft.Azure;

//using Microsoft.Azure.Storage.Blob;

using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;



//==========READ ME ================
//SEARCH FOR CONTOSO IN CODE AND SWITCH THE FILE NAME AND IPCOLUMN PARAMETERS WHERE EVER CONTOSO APPEARS
//IPCOLUMNNUMBER INDICATES THE COLUMN OF THE IP ADDRESS FIELD
//YOU CAN ALSO SWITCH OUT THE EVENT HUB WHERE YOU WANT THE PING RESULTS TRANSMITTED - SEARCH FOR CONTOSO2 IN THIS CASE

namespace PingAsync
{
    class Program
    {
        private static EventHubMessageSender _eventHubMessageSender;
        private static List<EventMessageModel> _eventMessageModels;

        //CONTOSO2 ===== 
        //Switch the connection string with your own event hub parameters - "Connection string–primary key"
        //"Connection string–primary key" string is found under "Shared access policies" tab under "Settings" on Azure Portal
        //Also remember to switch the eventHubName with your own eventHubName

      // private const string eventHubConnectionString = "Endpoint=sb://airjaldipingappns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1vr9oBgCU3vPIXiJ4QTGqXQvteBVV3njdXaphgBgFDE=";
      //private const string eventHubName = "airjaldipingapp";


        //==========================================
        //CONTOSO - INPUT THE FILE PATH HERE BY DRAG DROPPING YOUR FILE TO DATA FOLDER AND THEN BELOW FOR FULL PATH
        //private const string _FILE_NAME1 = @"C:\Air Jaldi\.NET3.0AsycCode\Asyn-r-code\PingAsync\Data\ipaddress.csv";
        //private const string _FILE_NAME2 = @"C:\Air Jaldi\.NET3.0AsycCode\Asyn-r-code\PingAsync\Data\ninehundredIPs.csv";
       // private const string _FILE_NAME3 = @"C:\Air Jaldi\.NET3.0AsycCode\Asyn-r-code\PingAsync\Data\WANIPAddressesSheet1.csv";
       // private const string _FILE_NAME4 = @"C:\Air Jaldi\.NET3.0AsycCode\Asyn-r-code\PingAsync\Data\resolvers.csv";

       // private const string _FILE_NAME5 = @"C:\Air Jaldi\.NET3.0AsycCode\Asyn-r-code\PingAsync\Data\ipaddress.xlsx";
       // private const string _FILE_NAME6 = @"C:\Air Jaldi\.NET3.0AsycCode\Asyn-r-code\PingAsync\Data\ninehundredIPs.xlsx";
       // private const string _FILE_NAME7 = @"C:\Air Jaldi\.NET3.0AsycCode\Asyn-r-code\PingAsync\Data\WANIPAddressesSheet1.xlsx";
      //private const string _FILE_NAME8 = @"C:\Air Jaldi\.NET3.0AsycCode\Komal\Asyn-r-code\PingAsync\Data\ipaddress.xlsx ";
        //===========================

        //CONTOSO - CHANGE THE POSITION of IP address Column, counting starts from 0 in case of csv but 1 in case of excel
        private const int _IP_COLUMN0 = 0;
        private const int _IP_COLUMN1 = 1;
        private const int _IP_COLUMN2 = 2;
        private const int _IP_COLUMN3 = 3;

        static private IConfiguration config;

        //CONTOSO - FILE EXTENSION TYPE _ EXCEL (.XLSX and CSV) allowed file types
        private const int _EXCEL_FILE = 1;
        private const int _CSV_FILE = 2;
        //private const string eventHubConnectionString = "Endpoint=sb://airjaldipingappns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=1vr9oBgCU3vPIXiJ4QTGqXQvteBVV3njdXaphgBgFDE=";
        //private const string eventHubName = "airjaldipingapp";

        //files 1,2,4 have ip addresses in the first column of the file hence they would have the _IP_COLUMN set to 1 
        //file 3 has the ip address column in the file to be the 3rd column hence the _IP_COLUMN value would be set to 3

        static void Main(string[] args)
        {
            //Code to read csv file from blob storage

            //string connectionString = CloudConfigurationManager.GetSetting("StorageConnectionString"); //blob connection string
            //string sourceContainerName = ConfigurationManager.AppSettings["sourcecontainerName"]; //source blob container name            
            //string sourceBlobFileName = "test.csv"; //source blob name



            //string connectionString = CloudConfigurationManager.GetSetting("StorageConnectionString"); //blob connection string
            //string sourceContainerName = ConfigurationManager.AppSettings["sourcecontainerName"]; //source blob container name  
            //string sourceBlobFileName = config["SELECTED_FILENAME"]; //source blob name
            //var csvData = GetCSVBlobData(sourceBlobFileName, connectionString, sourceContainerName);



            //*************************************************************************\\
            config = new ConfigurationBuilder()
         .AddJsonFile("appsettings.json", true, true)
         .Build();

            string eventHubConnectionString = FetchSecretValueFromKeyVault(GetToken()); // "Endpoint=sb://pingfunc.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=0CJEApfxuNJzRE0AoJcIX04InowYYYwmy0tKRkKDOnw=";
            string eventHubName = config["EventHub"];
           string blobAccountKey = FetchBlobKeySecretValueFromKeyVault(GetToken());

           // string blobAccountKey = config["BlobStorageAccountPrimaryKey"];

            string fileinblob = config["FILELOCATION_BLOB"];

            //if file is in blob storage get it and save it in local file storage
            if (fileinblob == "true")
            {
                string myAccountName = config["BlobStorageAccountName"];
                //string myAccountKey = config["BlobStorageAccountPrimaryKey"];
                string myAccountKey = blobAccountKey;
                string mycontainer = config["BlobStorageContainerName"];
                string myFileName = config["SELECTED_FILENAME"];
                string myFileSavePath = config["LOCAL_FILEPATH"] + "\\" + config["SELECTED_FILENAME"];


                var storageCredentials = new StorageCredentials(myAccountName, myAccountKey);
                var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
                var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

                var container = cloudBlobClient.GetContainerReference(mycontainer);
                container.CreateIfNotExistsAsync().Wait();


                var newBlob = container.GetBlockBlobReference(myFileName);
                newBlob.DownloadToFileAsync(myFileSavePath, FileMode.Create).Wait();

            }
            //*****************************************************\\

            for (int x = 0; x >= 0; x++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var connectionStringBuilder = new EventHubsConnectionStringBuilder(eventHubConnectionString)
                {
                    EntityPath = eventHubName
                };
                _eventHubMessageSender = new EventHubMessageSender(new EventHubConfiguration(eventHubConnectionString, eventHubName));
                _eventMessageModels = new List<EventMessageModel>();
                AsyncContext.Run(() => MainAsyncPing(args));
                // Console.WriteLine("Press ENTER to exit.");
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Console.Write("\n Finished Ping Results in ... " + elapsedMs + " milliseconds");

                var watchtwo = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine("\n Events Sent to Event Hub - {0} are {1} in total", eventHubName, _eventMessageModels.Count());
                AsyncContext.Run(() => MainAsyncEventHub(args));
                watchtwo.Stop();
                var elapsedMstwo = watchtwo.ElapsedMilliseconds;
                Console.Write("\n Finished sending events to Event Hub in ... " + elapsedMstwo + " milliseconds");

                Console.Write(" \n Iteration Number: #" + x + " \n");
                

                String sleepIntMin = config["PingFrequencyInterval"];
                int numvalSleepInt = 1;

                try
                {
                    numvalSleepInt = Convert.ToInt32(sleepIntMin);
                }
                catch (FormatException e)
                {
                    numvalSleepInt = 1;
                    Console.Write("\n Sleep Interval is not correct, please open appsettings.json to input a valid integer whole number value for minutes");
                }

                int sleepIntMs = numvalSleepInt * 60000;

                Thread.Sleep(sleepIntMs);
                Console.Write(" \n Sleeping for" + numvalSleepInt +"minutes \n");
            }

            Console.WriteLine("\n Press ENTER to exit.");
            Console.ReadLine();
        }

        static async Task MainAsyncEventHub(string[] args)
        { 
            //sending events to Event Hub
            await _eventHubMessageSender.SendAsync(_eventMessageModels);
        }

        static async Task MainAsyncPing(string[] args)
         {
            //function to asynchronously ping the ip address from the file
            try
            {
                //CONTOSO - change paramters according to file to be used above
                //files 1,2,4 have ip addresses in the first column of the file hence they would have the _IP_COLUMN set to 0 in case of csv and 1 in case of excel files 
                //file 3 has the ip address column in the file to be the 3rd column hence the _IP_COLUMN value would be set to value 3
                //CONTOSO - change paramters according to file to be used above, the ipaddress column and file type


                // var storageCredentials = new StorageCredentials("myAccountName", "myAccountKey");
                // var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
                // var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                //var container = cloudBlobClient.GetContainerReference("mycontainer");
                //await container.CreateIfNotExistsAsync();
                
                string filename = config["LOCAL_FILEPATH"] + "\\" + config["SELECTED_FILENAME"];
                string ipcolumn = config["SELECTED_IPCOLUMN"];
                int ipColumn = 0;

                if (ipcolumn == "_IP_COLUMN0")
                {
                    ipColumn = _IP_COLUMN0;
                }
                if (ipcolumn == "_IP_COLUMN1")
                {
                    ipColumn = _IP_COLUMN1;
                }
                if (ipcolumn == "_IP_COLUMN2")
                {
                    ipColumn = _IP_COLUMN2;
                }
                if (ipcolumn == "_IP_COLUMN3")
                {
                    ipColumn = _IP_COLUMN3;
                }


        // string filename = _FILE_NAME2;
        // int ipColumn = _IP_COLUMN0;

        string selectedfiletype = config["SELECTED_FILE_TYPE"];
                string filetype = "_EXCEL_FILE";

                if (selectedfiletype == "1")
                {
                    filetype = config["_FILE_TYPE1"];
                }
                if (selectedfiletype == "2")
                {
                    filetype = config["_FILE_TYPE2"];
                }

                string[] pingipargs = null;
                
                DataTable csvData = null;
                int totalcount = 0;

                if (filetype == "_EXCEL_FILE")
                {
                    pingipargs = ReadDataFrom(filename, ipColumn);

                    Console.WriteLine("Total IP(s) {0}", pingipargs.Count());

                    for (int row = 1; row < pingipargs.Count(); row++)
                    {
                        Console.WriteLine("{0}. {1}", row, pingipargs[row]);
                    }
                }
                else if (filetype == "_CSV_FILE")
                {
                    csvData = GetDataTabletFromCSVFile(filename);
                    Console.WriteLine("Hello Welcome to ping utility!");
                    Console.WriteLine("Rows count:" + csvData.Rows.Count);
                    pingipargs = new string[csvData.Rows.Count];
                    totalcount = csvData.Rows.Count;
                    //***********************************************************************/
                    //** Calling the print csv function to print data read from the csv **/
                    //**********************************************************************/ 
                    printIPList(csvData, ipColumn);
                    int ipnumber = 0;

                    //*********************************************************************************************************/
                    //** Creation of IP array list using a loop - currently being done asynchronously **/
                    //********************************************************************************************************/ 

                    foreach (DataRow dataRow in csvData.Rows)
                    {
                        string ipaddress = dataRow[ipColumn].ToString();
                        string arguments = ipaddress;
                        pingipargs[ipnumber] = arguments;
                        ipnumber++;
                    }
                }
                await PingWebsitesWithTCP(pingipargs, pingipargs.Count());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /*========================================*/
        //This function reads csv file from a blob

        /// <summary>
        /// GetCSVBlobData
        /// Gets the CSV file Blob data and returns a string
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="connectionString"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        private static string GetCSVBlobData(string filename, string connectionString, string containerName)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client.
           CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Retrieve reference to a blob named "test.csv"
            CloudBlockBlob blockBlobReference = container.GetBlockBlobReference(filename);

            string text;
            using (var memoryStream = new MemoryStream())
            {
                //downloads blob's content to a stream
                blockBlobReference.DownloadToStreamAsync(memoryStream);
                //blockBlobReference.DownloadToStream(memoryStream);

                //puts the byte arrays to a string
                text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            return text;
        }


        /*========================================*/
        //This function reads data from a excel file
        static public string[] ReadDataFrom(string workbookFilePath, int IPColumn)
        {
            string[] csvData = null;

            var workbookFileInfo = new FileInfo(workbookFilePath);

            using (ExcelPackage excelPackage = new ExcelPackage(workbookFileInfo))
            {
                var totalWorksheets = excelPackage.Workbook.Worksheets.Count;

                for (int sheetIndex = 1; sheetIndex <= totalWorksheets; sheetIndex++)
                {
                    var worksheet = excelPackage.Workbook.Worksheets[sheetIndex];
                    Console.WriteLine("Worksheet Name : {0}", worksheet.Name);

                    int rowCount = worksheet.Dimension.Rows;
                    int columnCount = worksheet.Dimension.Columns;

                    csvData = new string[worksheet.Dimension.Rows];

                    for (int rowIndex = 1; rowIndex <= rowCount; rowIndex++)
                    {
                        for (int columnIndex = 1; columnIndex <= columnCount; columnIndex++)
                        {
                            if ((columnIndex == IPColumn) && (rowIndex > 1))
                            {
                                var value = worksheet.Cells[rowIndex, columnIndex].Value.ToString();
                                //csvData[rowIndex - 1] = value + port22;
                                csvData[rowIndex - 1] = value;
                                // Console.WriteLine("IPAddress is Column {0}, Row{1} = {2}", columnIndex, rowIndex, value);
                            }
                        }
                    }
                }

            }
            return csvData;
        }

        //*********************************************************************************************/
        //** Function to extract data from csv and place it in datatable which will then be returned **/
        //*********************************************************************************************/ 
        private static DataTable GetDataTabletFromCSVFile(string csv_file_path)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csv_file_path))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;
                    string[] colFields = csvReader.ReadFields();
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        csvData.Columns.Add(datecolumn);
                    }
                    while (!csvReader.EndOfData)
                    {
                        string[] fieldData = csvReader.ReadFields();
                        //Making empty value as null
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == "")
                            {
                                fieldData[i] = null;
                            }
                            //Console.WriteLine(fieldData[i]);
                        }
                        csvData.Rows.Add(fieldData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("----Could not read the csv file, make sure it is in the proper format-------");
            }
            return csvData;
        }


        //*********************************************************************************************/
        //** Function to print only IP addresss column in the given DataTable**/
        //*********************************************************************************************/ 
        private static void printIPList(DataTable dt, int ipcolumnnumber)
        {

            int number = 1;
            Console.WriteLine("-------");
            foreach (DataRow dataRow in dt.Rows)
            {
                Console.WriteLine(number + ". " + dataRow[ipcolumnnumber]);
                number++;
            }

            Console.WriteLine("-------");
            return;
        }



        //=====================================================================================================

        static async Task PingWebsitesWithTCP(IEnumerable<string> iplist, int amountOfWebsites)
        {
            var enumeratedips = iplist.ToList();
            int ipid = 1;

            var tasks = new List<Task<PingReply>>();
            foreach (var ipadd in enumeratedips)
            {
                if (ipadd != null)
                {
                    string who = ipadd;
                    AutoResetEvent waiter = new AutoResetEvent(false);
                    Ping pingSender = new Ping();

                    // When the PingCompleted event is raised,
                    // the PingCompletedCallback method is called.
                    pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);
                    // Create a buffer of 32 bytes of data to be transmitted.
                    string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                    byte[] buffer = Encoding.ASCII.GetBytes(data);
                    // Wait 12 seconds for a reply.
                    int timeout = 4000;
                    // Set options for transmission:
                    // The data can go through 64 gateways or routers
                    // before it is destroyed, and the data packet
                    // cannot be fragmented.
                    PingOptions options = new PingOptions(64, false);
                    UserToken token = new UserToken();
                    token.Destination = who;
                    token.ipid = ipid;
                    ipid++;
                    token.waiter = new AutoResetEvent(false);
                    token.InitiatedTime = DateTime.Now;
                    pingSender.SendAsync(who, timeout, buffer, options, token);
                    waiter.WaitOne(50);
                }
            }
        }


        public class UserToken
        {
            public AutoResetEvent waiter { get; set; }
            public string Destination { get; set; }
            public int ipid { get; set; }
            public DateTime InitiatedTime { get; set; }
            public DateTime ReplyTime { get; set; }
        }

        private static void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            
            // If the operation was canceled, display a message to the user.
            if (e.Cancelled)
            {
                Console.WriteLine("Ping canceled.");

                // Let the main thread resume. 
                // UserToken is the AutoResetEvent object that the main thread 
                // is waiting for.
                ((AutoResetEvent)((UserToken)e.UserState).waiter).Set();
                //((AutoResetEvent)e.UserState).Set();
               
            }

            // If an error occurred, display the exception to the user.
            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(e.Error.ToString());

                // Let the main thread resume. 
               // ((AutoResetEvent)e.UserState).Set();
                ((AutoResetEvent)((UserToken)e.UserState).waiter).Set();

            }

            string result = "test";
            PingReply reply = e.Reply;
            string ipaddress = ((UserToken)e.UserState).Destination;
            int ipid = ((UserToken)e.UserState).ipid;
            Debug.Assert(true, string.Format("Reply from {0}", ((UserToken)e.UserState).Destination));

            if (e.Error != null)
            {
                result = JsonConvert.SerializeObject(reply);
            }
            else
            {
                result = JsonConvert.SerializeObject("Ping failed");
            }

            //result = JsonConvert.SerializeObject(reply);


            //Adding the Ping Reply to the event message list
            _eventMessageModels.Add(new EventMessageModel(reply, ipaddress, ipid));

            //display the reply
            DisplayReply(reply, ipaddress, ipid);
            // Let the main thread resume.
            ((AutoResetEvent)((UserToken)e.UserState).waiter).Set();
           
        }

        public static void DisplayReply(PingReply reply, string address, int ipid)
        {
            if (reply == null)
                return;
            Console.WriteLine("===============================");
            Console.WriteLine("{0}. Address: {1}", ipid, address);
            Console.WriteLine("ping status: {0}", reply.Status);
            // Console.WriteLine("Address: {0}", reply.Address.ToString());

            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
            }
        }

        #region "Get Eventhub connectionstring"
        static string GetToken()
        {
            WebRequest request = WebRequest.Create("http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fvault.azure.net");
            request.Headers.Add("Metadata", "true");
            WebResponse response = request.GetResponse();
            return ParseWebResponse(response, "access_token");
        }

        static string FetchSecretValueFromKeyVault(string token)
        {
            string keyvaulturl = config["KeyVault"];
            string secret = config["Secret"];
            WebRequest kvRequest = WebRequest.Create("https://" + keyvaulturl + "/secrets/" + secret + "?api-version=2016-10-01");
            kvRequest.Headers.Add("Authorization", "Bearer " + token);
            WebResponse kvResponse = kvRequest.GetResponse();
            return ParseWebResponse(kvResponse, "value");
        }

        static string FetchBlobKeySecretValueFromKeyVault(string token)
        {
            string keyvaulturl = config["KeyVault"];
            string secret = config["SecretTwo"];
            WebRequest kvRequest = WebRequest.Create("https://" + keyvaulturl + "/secrets/" + secret + "?api-version=2016-10-01");
            kvRequest.Headers.Add("Authorization", "Bearer " + token);
            WebResponse kvResponse = kvRequest.GetResponse();
            return ParseWebResponse(kvResponse, "value");
        }



        private static string ParseWebResponse(WebResponse response, string tokenName)
        {
            string token = String.Empty;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                String responseString = reader.ReadToEnd();

                JObject joResponse = JObject.Parse(responseString);
                JValue ojObject = (JValue)joResponse[tokenName];
                token = ojObject.Value.ToString();
            }
            return token;
        }
        #endregion

    }
}

