using MySql.Data;
using MySql.Data.MySqlClient;

namespace GetAllPlantTypes {
public class DbConnection
    {
        private DbConnection()
        {
        }

        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Port { get; set; }

        public MySqlConnection Connection { get; set; }

        private static DbConnection _instance = null;
        public static DbConnection Instance()
        {
            if (_instance == null)
                _instance = new DbConnection();
            return _instance;
        }

        public bool IsConnect()
        {
            if (Connection == null)
            {
                if (string.IsNullOrEmpty(DatabaseName))
                    return false;
                string connstring = string.Format("SERVER={0}; DATABASE={1}; UID={2}; PWD={3}; PORT={4}", Server, DatabaseName, UserName, Password, Port);
                Connection = new MySqlConnection(connstring);
                Connection.Open();
            }

            return true;
        }

        public void Close()
        {
            Connection.Close();
            Connection = null;
        }
    }
}