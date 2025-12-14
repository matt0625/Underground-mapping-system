using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TUBETREKWPFV1.Classes;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using TUBETREKWPFV1.Pathfinding;

namespace TUBETREKWPFV1.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var Name = Username.Text;
            var Pass = Password.Text;

            // generate hash of given input and for comparison to DB entries
            SHA256 sha256 = SHA256.Create();

            var PasswordBytes = Encoding.Default.GetBytes(Pass);

            var hashed = Convert.ToHexString(sha256.ComputeHash(PasswordBytes));

            using (UserDataContext context = new UserDataContext())
            {
                // true if any user with the matching credentials is found
                bool userfound = context.Users.Any(x => x.Username == Name && x.PasswordHash == hashed);

                if (userfound)
                {
                    // sets userid and username in session
                    GetUserID(Name);
                    UserSession.Username= Name;
                    GrantAccess();
                }
                else
                {
                    Username.Clear();
                    Password.Clear();

                    Username.Focus();

                    MessageBox.Show("User not found. Please try again.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                    Username.BorderBrush = Brushes.Red;
                    Password.BorderBrush = Brushes.Red;
                }
            }
        }

        // instantiate mainwindow object, show, and close this window 
        private void GrantAccess()
        {

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            this.Close();
        }

        // instantiate signupwindow object, show, and close this window 
        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            SignUpWindow signUpWindow = new SignUpWindow();
            signUpWindow.Show();
            this.Close();
        }

        // query database for user ID to set in session
        private void GetUserID(string username)
        {
            string dbFilePath = "Data Source=TubeTrekker.db";
            using (SqliteConnection conn = new SqliteConnection(dbFilePath))
            {
                conn.Open();

                string SQL = "SELECT UserID FROM Users WHERE Username = (@Username)";

                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@Username", username);

                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        UserSession.UserID = reader.GetInt32(0);
                    }

                }
                conn.Close();
            }
        }
    }
}
