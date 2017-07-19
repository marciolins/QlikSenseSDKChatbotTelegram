using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;

using QlikSenseEasy;


namespace Bot_Community
{
    class TelegramBot
    {
        // The Telegram Bot object
        // Here is where we insert the Bot Token
        private readonly TelegramBotClient Bot;

        // The Bot User Name
        static string cntBotName;

        // Users management
        private static string cntQlikUsersCSV = "QlikUsers.csv";
        private static QSUsers QlikUsers = new QSUsers();

        // Template QS App for new user sessions
        private static QSApp QSTemplateApp;

        // Predefined measures
        const string MeasDefSales = "Num(Sum({$<[Year]={\"2007\"}>}[Sales Amount]), '#,##0.00 €')";
        const string MeasDefMargin = "Num(Sum([Sales Margin Amount])/Sum([Sales Amount]), '0.0%')";
        const string MeasDefInventory = "Num(Sum([Inventory]), '#,##0')";
        const string MeasDefCost = "Num(Sum([Sales Amount])-Sum([Sales Margin Amount]), '#,##0.00 €')";



        public TelegramBot(string BotToken, QSApp TemplateApp)
        {
            // Save the template
            QSTemplateApp = TemplateApp;

            // Users
            QlikUsers.ReadFromCSV(cntQlikUsersCSV);
            Console.WriteLine("Users read");

            // The Telegram Bot object
            Bot = new TelegramBotClient(BotToken);

            // Set the calls that will receive the bot events
            SetBotCalls();

            // Start the Bot
            Bot.StartReceiving();
        }

        private void SetBotCalls()
        {
                // Initialize the Bot
                // Defining which functions are called for every Bot event

                // A message has been sent to the Bot
                Bot.OnMessage += BotOnMessageReceived;

                // A message has been edited
                Bot.OnMessageEdited += BotOnMessageReceived;

                // When the user push a button
                Bot.OnCallbackQuery += BotOnCallbackQueryReceived;

                // Timeout
                Bot.PollingTimeout = new TimeSpan(0, 0, 5); // 5 seconds

                var BotUser = Bot.GetMeAsync().Result;
                cntBotName = BotUser.Username;

                Console.Title = BotUser.Username;
                Console.WriteLine("Started and Callback set for Bot " + cntBotName);
        }

        public void CloseBot()
        {
            if(Bot.IsReceiving)
                Bot.StopReceiving();

            // Flush the users to the file
            QlikUsers.WriteToCSV(cntQlikUsersCSV);
        }


        public async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                var message = messageEventArgs.Message;
                string response = "";

                // Show the bot is working, as he was typing something
                BotShowTypingState(message.Chat.Id);

                if (message == null || message.Type == MessageType.ServiceMessage || message.Type == MessageType.UnknownMessage)
                    return;
                if (message.Entities.Count > 0 && message.Entities[0].Type == MessageEntityType.TextLink)
                    return;

                string MessageText = "";
                if (message.Text != null)
                    MessageText = message.Text.Trim().ToLower();    // Here the message to compare with the predefined commands

                // Show the message
                Console.WriteLine(string.Format("Message Received from {0}: {1}", message.From.Id.ToString(), MessageText));

                // Add the user if it not exists
                QSUser Usr = CheckTheUser(message.From.Id.ToString(),
                    message.From,
                    message.From.FirstName + " " + message.From.LastName);

                // Start some logic to answer the questions

                // This first block, only some basic predefined commands
                if (MessageText.Contains("sales"))
                {
                    string Val = Usr.QS.GetExpression(MeasDefSales);
                    response = "The value of sales is " + Val;
                }
                else if (MessageText.Contains("margin"))
                {
                    string Val = Usr.QS.GetExpression(MeasDefMargin);
                    response = "The value of margin is " + Val;
                }
                else if (MessageText.Contains("inventory"))
                {
                    string Val = Usr.QS.GetExpression(MeasDefInventory);
                    response = "The value of inventory is " + Val;
                }
                else if (MessageText.Contains("cost"))
                {
                    string Val = Usr.QS.GetExpression(MeasDefCost);
                    response = "The value of inventory is " + Val;
                }

                // Now we show a button menu, to allow the user select some KPIs
                else if (MessageText == "kpi") // request some kpis
                {
                    // Let's prepare a set of buttons with different kpis
                    // ready to touch and get the answer

                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] // first row
                        {
                            new InlineKeyboardButton("Sales", "#kpiSales"),
                            new InlineKeyboardButton("Margin", "#kpiMargin"),
                        },
                        new[] // second row
                        {
                            new InlineKeyboardButton("Inventory", "#kpiInventory"),
                            new InlineKeyboardButton("Cost", "#kpiCost")
                        },
                        new[] // second row
                        {
                            new InlineKeyboardButton("Full Analysis", "#kpiAnalysis")
                        }
                    });

                    await Bot.SendTextMessageAsync(message.Chat.Id, "Choose a Kpi", replyMarkup: keyboard);
                    return;
                }

                // It is important to be polite
                else if (MessageText.Contains("hello") || MessageText.StartsWith("hi"))
                {
                    response = "Hi " + Usr.UserName + ", how are you?";
                }
                else if (MessageText.Contains("bye"))
                {
                    response = "Bye " + Usr.UserName + "\n👋";
                }

                // Show basic help if needed
                else if (MessageText.Contains("help"))
                {
                    // Options not supported yet
                    // response = "I am sorry, but I do not know how to manage your message \"" + message.Text + "\"";
                    response = "This cool bot is connected to Qlik Sense to help you analyse your information.";
                    response += "\n\nUsage:\nkpi             - show a KPI\nSales             - Show the current sales value\nEtc.";
                }

                // If not a predefined command, let's try to find a measure from the text received
                else
                {
                    // If no other option, the user could have asked for a measure --> Try to find one and send the value
                    string FoundMeasure = "";
                    string val = Usr.QS.GetMeasureFormattedValue(MessageText, ref FoundMeasure);
                    response = "The value of " + FoundMeasure + " is " + val;
                }

                await BotSendTextMessage(message.Chat.Id, response);
                Console.WriteLine("Response: " + response);

            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("General Error in BotOnMessageReceived: {0}", e));
            }
        }

        private async void BotShowTypingState(long ChatId)
        {
            try
            {
                await Bot.SendChatActionAsync(ChatId, ChatAction.Typing);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("{0} Exception caught.", e));
            }
        }

        public async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            string msg;

            var chatMsg = callbackQueryEventArgs.CallbackQuery.Message;

            QSUser Usr = CheckTheUser(callbackQueryEventArgs.CallbackQuery.From.Id.ToString(),
                callbackQueryEventArgs.CallbackQuery.From,
                callbackQueryEventArgs.CallbackQuery.From.FirstName + " " + callbackQueryEventArgs.CallbackQuery.From.LastName);

            string ButtonId = callbackQueryEventArgs.CallbackQuery.Data;

            if (ButtonId.StartsWith("#kpi"))
            {
                // This is a KPI button
                string kpi = ButtonId.Substring(4);
                string val;

                switch (kpi.ToLower())
                {
                    // let's use lowercase for comparisons
                    case "sales":
                        val = Usr.QS.GetExpression("Num(Sum([Sales Quantity] * [Sales Price]), '#,##0.00 €')");
                        msg = "The value of Sales is " + val;
                        break;
                    case "margin":
                        val = Usr.QS.GetExpression("Num(Sum({<[Fiscal Year]={$(=(vCurrentYear)-1)}>}[Sales Amount]), '#,##0.00 €')");
                        msg = "The value of Margin is " + val;
                        break;
                    case "inventory":
                        val = Usr.QS.GetExpression("Num(((Sum([Sales Quantity]*[Sales Price]))-(Sum([Sales Cost Amount])))/(Sum([Sales Price]* [Sales Quantity])), '0.0%')");
                        msg = "The value of Inventory is " + val;
                        break;
                    case "cost":
                        val = Usr.QS.GetExpression("Num(Sum(ExpenseActual), '#,##0.00 €')");
                        msg = "The value of Cost is " + val;
                        break;
                    case "analysis":
                        string url = Usr.QS.qsSingleServer + "/sense/app/" + Usr.QS.qsSingleApp + "/sheet/" + Usr.QS.Sheets.First().Id + "/state/analysis"; // Open the Analysis sheet
                        msg = string.Format("{0}, <a href='{1}'>Here</a> you have a link to analyze all the information freely.", callbackQueryEventArgs.CallbackQuery.From.FirstName, url);

                        await BotSendTextMessage(chatMsg.Chat.Id, msg, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardHide());
                        return;

                    default:
                        msg = "The value of " + kpi + " is unknown";
                        break;
                }

                await BotSendTextMessage(chatMsg.Chat.Id, msg, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardHide());
            }
            else if (ButtonId.StartsWith("#measure"))
            {
                // This is a Measure button
                string Measure = ButtonId.Substring(8);
                string FoundMeasure = "";
                string val = Usr.QS.GetMeasureFormattedValue(Measure, ref FoundMeasure);
                msg = "The value of " + FoundMeasure + " is " + val;

                await BotSendTextMessage(chatMsg.Chat.Id, msg, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardHide());
            }
            else
                Console.WriteLine("Button command not supported");
        }


        // Common function to send a text message
        private async Task<Message> BotSendTextMessage(long chatId, string text, bool disableWebPagePreview = false, bool disableNotification = false, int replyToMessageId = 0, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Default, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (text.Trim() == "" || text == null) return null;             // Nothing to say, nothing to do
            if (text.Length > 4096) text = text.Substring(0, 4090) + "..."; // There is a limit of 4096 characters for Telegram messages

            Message m = new Message();

            try
            {                
                m = await Bot.SendTextMessageAsync(chatId, text, disableWebPagePreview, disableNotification, replyToMessageId, replyMarkup, parseMode, cancellationToken);
                Console.WriteLine("Sent text message <" + text + "> to ChatID " + chatId.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Failed send text message. {0} Exception caught.", e));
            }
            return m;
        }


        /*********************************************************************************************************
        // This function will check the user is already known or it will be added
        // It will also do any check to be sure the user is ready, for example test the Qlik Sense connection
        *********************************************************************************************************/
        private static QSUser CheckTheUser(string UserId, Telegram.Bot.Types.User TelegramUser, string UserName = "")
        {
            // Add the user if not exists
            // All new users are allowed by default except if running in Demo Mode, they can be banned in the users file
            QSUser Usr = QlikUsers.AddUser(UserId, UserName);

            if (Usr.QS == null)
            {   // This user has no connection
                Usr.QS = new QSApp();

                // This is the default Qlik Sense app for new users
                // Fill it with the default values from the template
                Usr.QS.qsAppName = QSTemplateApp.qsAppName;
                Usr.QS.qsAppId = QSTemplateApp.qsAppId;
                Usr.QS.qsServer = QSTemplateApp.qsServer;
                Usr.QS.qsSingleServer = QSTemplateApp.qsSingleServer;
                Usr.QS.qsSingleApp = QSTemplateApp.qsSingleApp;
                Usr.QS.qsAlternativeStreams = QSTemplateApp.qsAlternativeStreams;


                try
                {
                    // Create or use the Telegram UserID as the Qlik Sense UserID to open the connection
                    Connect(Usr.QS, UserId, QSTemplateApp.VirtualProxy(), QSTemplateApp.IsUsingSSL());
                    // And open the app
                    Usr.QS.QSOpenApp();

                    Console.WriteLine(string.Format("Opened the Qlik Sense app: {0} for user {1} ({2})", Usr.QS.qsAppName, Usr.UserId, Usr.UserName));
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("Error opening the Qlik Sense app{0} for user {1}: {2}", Usr.QS.qsAppName, Usr.UserId, e));
                }
            }
            else
            {
                // The connection was previously created, so check the connection is alive
                Usr.QS.CheckConnection();
            }

            return Usr;
        }

        private static void Connect(QSApp TheQS, string UserId, string VirtualProxyPath = "", bool UseSSL = true)
        {   // Connects a QSApp object
            try
            {
                // Create or use the Telegram UserID as the Qlik Sense UserID
                // With the bot virtual proxy and user directory
                // The Virtual Proxy has to be created as "Header authentication static user directory"
                // The user directory is defined in the Virtual Proxy, in "Header authentication static user directory"
                // For this example:
                //
                // Qlik Sense Virtual Proxy
                // ------------------------
                //
                // Description: Telegram
                // Prefix: telegram
                // Timeout: 30
                // Session cookie header name: X-Qlik-Session-telegram
                // Anonymous access mode: No anonymous user
                // Authentication method: Header authentication static user directory
                // Header authentication header name: X-Qlik-HeaderAuthTelegram
                // Header authentication static user directory: TELEGRAM
                // 
                // PD.- Do not forget to add the server node, link the proxy and include the server name in the white list

                TheQS.QSConnectServerHeader(UserId, "X-Qlik-HeaderAuthTelegram", VirtualProxyPath, UseSSL, true);


                if (TheQS.IsConnected)
                {
                    TheQS.QSOpenApp();
                    if (TheQS.AppIsOpen)
                    {
                        Console.WriteLine(string.Format("Opened the Qlik Sense app: {0} for user {1} ({2})", TheQS.qsAppId, UserId, VirtualProxyPath));

                        //string Val = TheQS.GetExpression("Sum([Sales Amount])");
                        //Console.WriteLine("Value: " + Val);
                    }
                }


            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Error opening the Qlik Sense app: {0} for user {1} ({2}): {3}", TheQS.qsAppId, UserId, VirtualProxyPath, e));
            }

        }


    }
}
