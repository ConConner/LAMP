using System;
using System.Net.Http;
using System.Windows.Forms;

namespace LAMP;

internal static class Program
 {
     //Creating a Webclient for the entire application
     public static HttpClient httpClient = new HttpClient();
 
     /// <summary>
     ///  The main entry point for the application.
     /// </summary>
     [STAThread]
     private static void Main()
     {
         Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         Application.Run(new MainWindow());
     }
 }