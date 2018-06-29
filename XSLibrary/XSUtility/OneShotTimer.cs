﻿using System;

namespace XSLibrary.Utility
{
    public class OneShotTimer
    {
        DateTime m_start;
        bool m_started = false;
        bool m_timerOverflow;
        long m_timerEnd;

        public TimeSpan TimePassed { get { return m_started ? DateTime.Now - m_start : new TimeSpan(0); } }

        public OneShotTimer(long microSeconds, bool start = true)
        {
            TimerInterval(microSeconds);

            if (start)
                Restart();
        }

        public void TimerInterval(long microSeconds)
        {
            m_timerEnd = microSeconds;
            m_timerOverflow = false;
        }

        public void Restart()
        {
            m_start = DateTime.Now;
            m_started = true;
            m_timerOverflow = false;
        }

        private bool TimerOverflow()
        {
            if (!m_started)
                return false;

            if (m_timerOverflow)
                return true;

            if(m_start.Ticks + (m_timerEnd * 10) < DateTime.Now.Ticks)
                m_timerOverflow = true;

            return m_timerOverflow;
        }

        public static bool operator ==(OneShotTimer timer, bool val)
        {
            return timer.TimerOverflow() == val;
        }

        public static bool operator !=(OneShotTimer timer, bool val)
        {
            return timer != val;
        }
        
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
        public override bool Equals(object o)
        {
            throw new NotImplementedException();
        }
    }
}
