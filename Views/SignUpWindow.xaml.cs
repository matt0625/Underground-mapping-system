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
using System.Security.Cryptography;
using System.Runtime.Intrinsics.Arm;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TUBETREKWPFV1.Views
{
    public partial class SignUpWindow : Window
    {
        public SignUpWindow()
        {
            InitializeComponent();
        }

        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            var Username = UsernameTextBox.Text;
            var Password = PasswordBox.Text;
            var PasswordCheck = ConfirmPassword.Text;


            // validation of inputs before proceeding to register user 
            if (!(Password == PasswordCheck))
            {
                ReturnInputError("Passwords do not match");
            }
            else if (!UsernameRegexMatch(Username))
            {
                ReturnInputError("Username does not meet requirements\n" + "Click the ?");
            }
            else if (!PasswordRegexMatch(Password))
            {
                ReturnInputError("Password does not meet requirements\n" + "Click the ?");
            }
            else if (!CheckUsernameUnique(Username))
            {
                ReturnInputError("Username has been taken :(");
            }
            else
            {
                // create salt of password and store instead
                SHA256 sHA256 = SHA256.Create();
                var PasswordBytes = Encoding.Default.GetBytes(Password);
                var hashed = Convert.ToHexString(sHA256.ComputeHash(PasswordBytes));

                RegisterUser(Username, hashed);


                var loginWindow = new LoginWindow();
                loginWindow.Show();

                this.Close();
            }
            
        }

        // register user's username and passwordhash (userID is autoincrement) 
        private void RegisterUser(string Username, string PasswordHash)
        {
            string dbFilePath = "Data Source=TubeTrekker.db";
            using (SqliteConnection conn = new SqliteConnection(dbFilePath))
            {
                conn.Open();

                string SQL = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @Hash)";

                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@Username", Username);
                    cmd.Parameters.AddWithValue("@Hash", PasswordHash); 

                    // 'ExecuteNonQuery' as INSERT INTO does not return a value
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            } 
        }

        private bool UsernameRegexMatch(string Username)
        {
            // pattern defines the set of acceptable usernames
            // usernames must contain at least 8 characters (upper or lower case) and at least 1 digit
            // importantly, usernames which are just strings of digits are not accepted
            string pattern = @"^(?=.*[a-zA-Z])(?=.*\d).{8,20}";
            Regex rg = new Regex(pattern);
            return rg.IsMatch(Username);
        }

        private bool PasswordRegexMatch(string Password)
        {
            // building upon UsernameRegexMatch's pattern, we now require at least one of some prespecified special characters
            // i also separate the upper OR lower case requirement to an upper AND lower case lookahead to further restrict valid strings
            string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!/'><.,?#\%@-^&+]).{8,20}";
            Regex rg = new Regex(pattern);
            return rg.IsMatch(Password);
        }

        private void ReturnInputError(string message)
        {
            UsernameTextBox.Clear();
            PasswordBox.Clear();
            ConfirmPassword.Clear();


            UsernameTextBox.Focus();

            // displays the appropriate given message for each case 
            MessageBox.Show(message, "Sign up Failed", MessageBoxButton.OK, MessageBoxImage.Warning);

            UsernameTextBox.BorderBrush = Brushes.Red;
            PasswordBox.BorderBrush = Brushes.Red;
            ConfirmPassword.BorderBrush = Brushes.Red;
        }

        private void UsernameDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Username must contain 8 characters and 1 digit", "Username Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PasswordDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            string message = "Password must contain\n" +
                 "• 8 characters\n" +
                 "• 1 Upper case character\n" +
                 "• 1 Lower case character\n" +
                 "• 1 Digit\n" +
                 "• 1 of the following: ! / ' > < . , ? # \\ % @ - ^ & +\n";
            MessageBox.Show(message, "Password Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // querying the database to ensure the username entered is unique 
        private bool CheckUsernameUnique(string Username)
        {
            string dbFilePath = "Data Source=TubeTrekker.db";
            using (SqliteConnection conn = new SqliteConnection(dbFilePath))
            {
                conn.Open();

                string SQL = "SELECT Username FROM Users WHERE Username = (@Username)";

                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    cmd.Parameters.AddWithValue("@Username", Username);
                    // 'ExecuteReader' as SELECT may return a value
                    var result = cmd.ExecuteReader();
                    // check if the result of query contains a record 
                    // if it does then username not unique
                    if (result.Read())
                    {
                        return false;
                    }
                }

                conn.Close();
            }
            return true;

        }

        private void ReturnToLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            this.Close();
        }
    }
}
