﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DTUServiceMonitor
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ConfigurationAdapter.Initial();
            Application.Run(new Form1());
            ConfigurationAdapter.Dispose();
        }
    }
}
