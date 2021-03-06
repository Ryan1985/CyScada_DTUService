﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTUServiceMonitor
{
    public class Logger
    {
        private static Queue<string> logStringQueue = new Queue<string>(10);
        private static object m_lock = new object();


        public static void Enqueue(string logString)
        {
            lock (m_lock)
            {
                logStringQueue.Enqueue(logString);
            }
        }

        public static string Dequeue()
        {
            lock (m_lock)
            {
                return logStringQueue.Count >= 0 ? logStringQueue.Dequeue() : null;
            }
        }

        public static int Count
        {
            get
            {
                return logStringQueue.Count;
            }
        }


    }
}
