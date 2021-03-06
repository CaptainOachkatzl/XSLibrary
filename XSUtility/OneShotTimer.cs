﻿using System;

namespace XSLibrary.Utility
{
    public class OneShotTimer
    {
        DateTime m_start;
        public bool Started { get; private set; } = false;
        bool m_timerOverflow;
        long m_timerEnd;

        public TimeSpan TimePassed { get { return Started ? DateTime.Now - m_start : new TimeSpan(0); } }
        public TimeSpan TimeLeft { get { return new TimeSpan(m_timerEnd * 10 - TimePassed.Ticks); } }

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
            Reset();
            Start();
        }

        public void Reset()
        {
            m_timerOverflow = false;
            Started = false;
        }

        private void Start()
        {
            m_start = DateTime.Now;
            Started = true;
        }

        private bool TimerOverflow()
        {
            if (!Started)
                return false;

            if (m_timerOverflow)
                return true;

            if(m_start.Ticks + (m_timerEnd * 10) < DateTime.Now.Ticks)
                m_timerOverflow = true;

            return m_timerOverflow;
        }

        public static implicit operator bool(OneShotTimer timer)
        {
            return timer.TimerOverflow();
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
