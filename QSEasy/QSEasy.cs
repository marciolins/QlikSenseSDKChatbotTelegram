using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Qlik.Engine;
using Qlik.Sense.Client;
using Qlik.Sense.Client.Visualizations;
using Qlik.Sense.Client.Visualizations.Components;

using NinjaNye.SearchExtensions;

/*********************************************************************************************************************/
/*********************************************************************************************************************/
// Qlik Sense Easy
// ---------------
// 
// This is a library with functions to access a Qlik Sense server from .Net
// It is based on the Qlik Sense .Net SDK (http://help.qlik.com/en-US/sense-developer/June2017/Subsystems/NetSDKAPI/Content/Introduction/Net-Sdk-Intro.htm)
// I have tried to ease the integration with simpler functions
// 
// Author: Juan Gerardo Cabeza (https://www.linkedin.com/in/juan-gerardo-cabeza-a414b825/)
//
// Date:   July 2017
// Last Qlik Sense version tested: June 2017
/*********************************************************************************************************************/
/*********************************************************************************************************************/


namespace QlikSenseEasy
{
    // Class with he main properties for a Master Item
    public class QSMasterItem
    {
        public string Name { get; set; }
        public string Expression { get; set; }
        public string Id { get; set; }
        public string FormattedExpression { get; set; }
        public IEnumerable<string> Tags { get; set; }

    }

    // Class with he main properties for a Story
    public class QSStory
    {
        public string Name { get; set; }
        public string Id { get; set; }
        //public string FirstSlideId { get; set; }
    }

    // Class with he main properties for a Sheet
    public class QSSheet
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }

    // Class with he main properties for a Stream
    public class QSStreamProperties
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }


    // Class with he main properties for an App
    public class QSAppProperties
    {
        public DateTime modifiedDate { get; set; }
        public bool published { get; set; }
        public DateTime publishTime { get; set; }
        public IList<string> privileges { get; set; }
        public string description { get; set; }
        public int qFileSize { get; set; }
        public string dynamicColor { get; set; }
        public object create { get; set; }
        public QSStreamProperties stream { get; set; }
        public bool canCreateDataConnections { get; set; }
        public string AppID { get; set; }
        public string AppName { get; set; }
        public string AppTitle { get; set; }
        public string ThumbnailUrl { get; set; }
    }


    // Main class, all is based on an App --> Connection and App opening
    public class QSApp
    {
        // Main variables for the Qlik Sense connection
        public string qsServer { get; set; }

        // Virtual Proxy --> Static Header --> Header Authentication Name
        private string QSHeaderAuthName;

        // The Qlik Sense .Net SDK variables for the server and the app
        private ILocation qsLocation;
        private IApp qsApp;

        // This variables is to build a single object link, to access an object directly
        public string qsSingleServer { get; set; }
        public string qsSingleApp { get; set; }

        // Main properties for the App
        public string qsAppName { get; set; }
        public string qsAppId { get; set; }
        public string qsAppThumbnailUrl { get; set; }

        // List of other apps to switch, alternative apps the user can open
        public List<QSAppProperties> qsAlternativeApps = new List<QSAppProperties>();
        public string qsAlternativeStreams = null;

        // Auxiliary variables
        const int maxFounds = 10;
        private static Random rnd;


        // Master Items
        public List<QSMasterItem> MasterMeasures = new List<QSMasterItem>();
        public List<QSMasterItem> MasterDimensions = new List<QSMasterItem>();
        public List<QSMasterItem> MasterVisualizations = new List<QSMasterItem>();

        // Stories
        public List<QSStory> Stories = new List<QSStory>();

        // Sheets
        public List<QSSheet> Sheets = new List<QSSheet>();

        // Last queries
        public QSMasterItem LastMeasure;
        public QSMasterItem LastDimension;

        // User connected, to remember for future reconnections
        private string QSUserId;
        private string QSUserDir;

        // Variable to know if the connection is OK
        private bool QSIsConnected = false;
        // And the correspondent property
        public bool IsConnected { get { return QSIsConnected; } }


        // Variable to know if the App could be open
        private bool QSAppIsOpen = false;
        // And the correspondent property
        public bool AppIsOpen { get { return QSAppIsOpen; } }


        public QSApp()
        {
            // Default values and initializations
            qsServer = "http://desktop-e106r7e";
            qsAppName = "Consumer Goods Sales";
            qsAppId = "787cc63e-3286-4d5b-a673-aaa3d5b86b8c";
            qsSingleServer = "http://desktop-e106r7e";
            qsSingleApp = "787cc63e-3286-4d5b-a673-aaa3d5b86b8c";

            rnd = new Random();
        }


        public void CheckConnection()
        {   // Check if it is connected to the Qlik Sense server, if not, reconnect.
            if (!qsLocation.IsAlive())
            {
                // Reconnect
                // Header authentication
                this.QSConnectServerHeader(QSUserId, QSHeaderAuthName, qsLocation.VirtualProxyPath, IsUsingSSL(), IsCheckingSDKVersion());
            }
        }


        // Connects to the Qlik Sense server with the UserID and UserDirectory provided (Qlik Sense users)
        public void QSConnectServerHeader(string UserId, string HeaderAuthName, string VirtualProxyPath = "",
            Boolean UseSSL = false, Boolean CheckSDKVersion = true)
        {
            QSIsConnected = false;
            string strUri = qsServer;
            Uri uri = new Uri(strUri);

            qsLocation = Qlik.Engine.Location.FromUri(uri);
            if (VirtualProxyPath.Trim() != "") qsLocation.VirtualProxyPath = VirtualProxyPath;

            qsLocation.AsStaticHeaderUserViaProxy(UserId, HeaderAuthName, UseSSL);

            qsLocation.IsVersionCheckActive = CheckSDKVersion;
            IHub MyHub = qsLocation.Hub();

            QSUserId = UserId;
            QSHeaderAuthName = HeaderAuthName;

            QSIsConnected = true;

            Console.WriteLine("QSEasy connected to Qlik Sense version: " + MyHub.ProductVersion());
            Console.WriteLine("UserID: " + UserId + " - VirtualProxy: " + VirtualProxyPath);
        }


        // Returns if the connection is checking the SDK version
        public bool IsCheckingSDKVersion()
        {
            return qsLocation.IsVersionCheckActive;
        }

        // Returns if the connection is using SSL
        public bool IsUsingSSL()
        {
            return (qsLocation.ServerUri.Scheme == "https");
        }

        // Returns the virtual proxy used for the connection
        public string VirtualProxy()
        {
            if (qsLocation.VirtualProxyPath == null || qsLocation.VirtualProxyPath.Trim() == "")
                return "";
            else
                return qsLocation.VirtualProxyPath;
        }

        // Open a Qlik Sense app with the properties defined in this class
        public void QSOpenApp()
        {
            QSAppIsOpen = false;

            try
            {
                CheckConnection();

                IAppIdentifier MyAppId;
                if (qsAppId != "" && qsAppId != null)
                {
                    MyAppId = qsLocation.AppWithIdOrDefault(qsAppId);
                }
                else
                {
                    MyAppId = qsLocation.AppWithNameOrDefault(qsAppName);
                }

                qsAppId = MyAppId.AppId;
                qsAppName = MyAppId.AppName;

                qsApp = qsLocation.App(MyAppId);
                qsAppThumbnailUrl = qsApp.GetAppProperties().Thumbnail.Url;

                // Later
                //QSReadFields();
                QSReadMasterItems();
                QSReadSheets();
                //QSReadStories();
                //GetAlternativeApps(qsAlternativeStreams); // Refresh the alternatives every time I open an app

                QSAppIsOpen = true;

                Console.WriteLine("QSEasy opened App " + (qsAppId != "" ? qsAppId : qsAppName) + " with handle: " + qsApp.Handle);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                QSAppIsOpen = false;
            }

        }

        // Open the Qlik Sense App defined in the parameter
        private void QSOpenApp(QSAppProperties App)
        {
            qsAppName = App.AppName;
            qsAppId = App.AppID;
            qsSingleApp = App.AppID;
            qsAppThumbnailUrl = App.ThumbnailUrl;

            QSOpenApp();
        }

        // Open the Qlik Sense App with Id = AppId, if it is in the alternative apps
        public void QSOpenApp(string AppId)
        {
            QSAppProperties AppProp = new QSAppProperties();
            AppProp.AppID = AppId;
            QSOpenApp(AppProp);
        }


        /**********************************************/
        /**********************************************/
        // Functions to get master info from Qlik Sense
        /**********************************************/
        /**********************************************/

        private void QSReadSheets()
        {
            // Read all the sheets in a list, to ease find the correct sheet when requested, as it is defined in the app
            Sheets.Clear();
            foreach (Qlik.Sense.Client.ISheet AppSheet in Qlik.Sense.Client.AppExtensions.GetSheets(qsApp))
            {
                QSSheet qss = new QSSheet();
                qss.Id = AppSheet.Id;
                qss.Name = AppSheet.Properties.MetaDef.Title;
                var m = AppSheet.Properties.MetaDef;
                Sheets.Add(qss);
            }
        }

        private void QSReadMasterItems()
        {
            // Get all the Master Visualizations
            MasterVisualizations.Clear();
            try
            {
                var allMasterObjects = qsApp.GetMasterObjectList().Items?.Select(item => qsApp.GetObject<MasterObject>(item.Info.Id));

                foreach (IMasterObject mo in allMasterObjects)
                {
                    QSMasterItem mi = new QSMasterItem();
                    var properties = mo.Properties;
                    mi.Id = properties.Info.Id;
                    mi.Name = properties.MetaDef.Title;
                    mi.Tags = mo.MetaAttributes.Tags;
                    MasterVisualizations.Add(mi);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("QSEasy Error in QSReadMasterItems: {0} Exception caught.", e);
            }

            // Get all the Master Measures
            MasterMeasures.Clear();
            try
            {
                // Get all the Master Measures
                var allMeasures = qsApp.GetMeasureList().Items;

                foreach (IMeasureObjectViewListContainer mm in allMeasures)
                {
                    QSMasterItem mi = new QSMasterItem();
                    INxLibraryMeasureDef md = qsApp.GetMeasure(mm.Info.Id).NxLibraryMeasureDef;
                    mi.Id = mm.Info.Id;
                    mi.Name = md.Label;
                    mi.Expression = md.Def;
                    // In FormattedExpression I get the total value already calculated
                    mi.FormattedExpression = GetExpressionFormattedValue(Expression: mi.Expression, Label: mi.Name);
                    MasterMeasures.Add(mi);
                }
                LastMeasure = MasterMeasures.Count > 0 ? MasterMeasures.First() : null;
            }
            catch (Exception e)
            {
                Console.WriteLine("QSEasy Error in QSReadMasterItems: {0} Exception caught.", e);
            }

            // Get all the Master Dimensions
            MasterDimensions.Clear();
            try
            {
                // Get all the Master Dimensions
                var allDimensions = qsApp.GetDimensionList().Items;

                foreach (DimensionObjectViewListContainer md in allDimensions)
                {
                    if (md.Data.Grouping == NxGrpType.GRP_NX_NONE)  // This is to avoid drill-down dimensions, I only use single dimensions
                    {
                        QSMasterItem mi = new QSMasterItem();

                        INxLibraryDimensionDef dd = qsApp.GetDimension(md.Info.Id).NxLibraryDimensionDef;
                        mi.Id = md.Info.Id;
                        mi.Name = md.Data.Title;
                        mi.Expression = dd.FieldDefs.First();
                        MasterDimensions.Add(mi);
                    }
                }
                LastDimension = MasterDimensions.Count > 0 ? MasterDimensions.First() : null;
            }
            catch (Exception e)
            {
                Console.WriteLine("QSEasy Error in QSReadMasterItems: {0} Exception caught.", e);
            }
        }


        // This function looks for the master measure most similar to MeasureName
        public QSMasterItem GetMasterMeasure(string MeasureName)
        {
            // Look for a measure in the app master measures, and return the name used in the app
            QSMasterItem meas = MasterMeasures.Find(m => m.Name.ToLower().Trim() == MeasureName.ToLower().Trim());

            if (meas == null)
            {
                // If not an exact match, try to find a master measure that contains the measure searched
                meas = MasterMeasures.Find(m => m.Name.ToLower().Trim().Contains(MeasureName.ToLower().Trim()));
            }
            if (meas == null)
            {
                // Search based on similarity, using the Levenshteing algorithm, from the NinjaNye.SearchExtensions library
                var result = MasterMeasures.LevenshteinDistanceOf(m => m.Name)
                    .ComparedTo(MeasureName)
                    .OrderBy(m => m.Distance);
                if (result.Count() > 0)
                    meas = (QSMasterItem)result.First().Item;
            }

            if (meas != null)
            {
                // Evaluate and return the master measure expression
                LastMeasure = meas;
                return meas;
            }
            else
            {
                return null;
            }
        }




        /****************************************/
        /****************************************/
        // Functions to get data from Qlik Sense
        /****************************************/
        /****************************************/

        // Returns the result of evaluating a Qlik Sense expression, for example "Sum(Sales)" as a String
        public string GetExpression(string Expression)
        {
            string exp;

            try
            {
                exp = qsApp.Evaluate(Expression);
            }
            catch (Exception e)
            {
                Console.WriteLine("QSEasy Error: {0} Exception caught.", e);
                exp = "";
            }

            if (Expression == "" || exp.StartsWith("Error:"))
            {
                exp = "";
            }

            return (exp);
        }

        // Returns the result of evaluating a Qlik Sense expression, for example "Sum(Sales)" as a Number
        public double GetExpressionValue(string Expression)
        {
            double val;
            FieldValue exp;

            try
            {
                exp = qsApp.EvaluateEx(Expression);
                if (exp.IsNumeric)
                    val = exp.Number;
                else
                    val = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("QSEasy Error: {0} Exception caught.", e);
                val = 0;
            }

            return (val);
        }

        public string GetExpressionFormattedValue(string Expression, string Label = "")
        {
            // Same as GetExpressionValue(), but return a formatted string instead of the numeric value
            return FormatValue(GetExpressionValue(Expression), Label);
        }

        public string GetMeasureFormattedValue(string MeasureName, ref string FoundMeasure)
        {
            // Same as GetExpressionFormattedValue(), but for a specific Master Measure
            QSMasterItem Measure = GetMasterMeasure(MeasureName);

            if (Measure == null)
                return MeasureName + " has not been found in Measures";
            else
            {
                FoundMeasure = Measure.Name;
                return Measure.FormattedExpression; // Get the total value directly from the Measure object
            }
        }


        // This function finds the most appropiate string format for a numeric value
        private string FormatValue(double Value, string Label = "")
        {
            string strValue = null;

            if (Value != 0)
            {
                if (Label.Contains("%")) strValue = Value.ToString("P1");
                else if (Value > 0 && Value < 1) strValue = Value.ToString("P1");
                else if (Label.Contains("€") || Label.Contains("$")) strValue = Value.ToString("C2");
                else strValue = Value.ToString("N2");
            }
            else
            {
                strValue = "0";
            }
            return strValue;
        }



    }

}
