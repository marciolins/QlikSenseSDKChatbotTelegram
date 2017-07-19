using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using QlikSenseEasy;


namespace Bot_Community
{
    class QSUsers
    {
        private static List<QSUser> Users;

        public QSUsers()
        {
            Users = new List<QSUser>();
        }

        public List<QSUser> GetUserList()
        {
            return (Users);
        }

        public void ReadFromCSV(string FileName)
        {
            List<QSUser> values;
            try
            {
                if (!File.Exists(FileName))
                    File.Create(FileName);

                values = File.ReadAllLines(FileName, System.Text.Encoding.Default)
                                            .Skip(0)
                                            .Select(v => QSUser.FromCsv(v))
                                            .ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in QSUsers with file \"{0}\": {1} Exception caught.", FileName, e);
                values = null;
            }

            if (values != null) Users = values;
        }

        public void WriteToCSV(string FileName)
        {
            try
            {
                File.WriteAllLines(FileName, Users.Select(u => QSUser.ToCsv(u)), System.Text.Encoding.Default);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in QSUsers writing to file \"{0}\": {1} Exception caught.", FileName, e);
            }
        }

        public QSUser GetUser(string UserId)
        {
            QSUser result;
            try
            {
                result = Users.Find(x => x.UserId.ToLower() == UserId.ToLower().Trim());
            }
            catch (Exception e)
            {
                result = null;
            }

            if (result != null) return result;
            else return null;
        }

        public QSUser AddUser(string UserId, string UserName = "")
        {
            QSUser Usr;

            try
            {
                Usr = GetUser(UserId);

                if (Usr == null)
                {
                    // New User
                    Usr = new QSUser();
                    Usr.UserId = UserId.Trim();
                    Users.Add(Usr);
                }

                // Update properties
                if (Usr.UserName == "" || UserName != "") Usr.UserName = UserName.Trim();

            }
            catch (Exception e)
            {
                Console.WriteLine("Error in AddUser with UserId \"{0}\": {1} Exception caught.", UserId, e);
                return null;
            }

            return Usr;
        }
    }

    public class QSUser
    {
        public string UserId;
        public string UserName;
        public QSApp QS;

        public static QSUser FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(';');
            QSUser U = new QSUser();

            U.UserId = values[0].ToLower().Trim();  // To compare case insensitive
            U.UserName = values[1].Trim();

            return U;
        }

        public static string ToCsv(QSUser U)
        {
            string csvLine;

            csvLine = string.Format("{0};{1}", U.UserId, U.UserName);

            return csvLine;
        }

    }
}
