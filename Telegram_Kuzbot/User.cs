//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Text;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace Telegram_Kuzbot
//{
//    public class Connection
//    {
//        public static string SqlConnectionSQLServer
//        {
//            get
//            {
//                var sb = new SqlConnectionStringBuilder
//                {
//                    DataSource = "LAPTOP-U9N8U34L",
//                    IntegratedSecurity = true,
//                    InitialCatalog = "KuzBotDB"
//                };

//                return sb.ConnectionString;
//            }
//        }
//    }

//    public class User
//    {
//        public int ID { get; set; }
//        public string ChatID { get; set; }
//        public string UserID { get; set; }
//        public string FirstName { get; set; }
//    }

//    public class DataContext : DbContext
//    {
//        public DbSet<User> Users { get; set; }
//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            optionsBuilder.UseSqlServer(Connection.SqlConnectionSQLServer);
//        }
//    }
//}