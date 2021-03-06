﻿using XSLibrary.ThreadSafety.Executors;

namespace XSLibrary.ThreadSafety.Events
{
    /// <summary>
    /// Triggers if the event is invoked or was invoked before subscribing to it.
    /// <para> Can be accessed safely by multiple threads.</para>
    /// </summary>
    public class OneShotEvent<Sender, Args> : IEvent<Sender, Args>
    {
        /// <summary>
        /// Handle will be invoked if the event was triggered in the past.
        /// <para>Unsubscribing happens automatically after the invocation and is redundant if done from the event handle.</para>
        /// </summary>
        public sealed override event EventHandle Event
        {
            add
            {
                if (!Subscribe(value))
                    value(m_sender, m_eventArgs);
            }
            remove { InternalEvent -= value; }
        }

        private event EventHandle InternalEvent;

        private SafeReadWriteExecutor m_lock = new RWExecutorWinNative();
        private bool m_invoked = false;

        Sender m_sender;
        Args m_eventArgs;

        /// <summary>
        /// Invokes all subscribed handles with the given parameters. 
        /// <para>All calls after the first are ignored.</para>
        /// </summary>
        public void Invoke(Sender sender, Args args)
        {
            GetEventHandle(sender, args)?.Invoke(m_sender, m_eventArgs);
        }

        public sealed override IEvent<Relay, Args> CreateRelay<Relay>(Relay sender)
        {
            return new EventRelay<Relay, Sender, Args>(sender, this);
        }

        private EventHandle GetEventHandle(Sender sender, Args args)
        {
            return m_lock.Execute(() =>
            {
                if (m_invoked)
                    return null;

                m_sender = sender;
                m_eventArgs = args;
                m_invoked = true;
                EventHandle handle = InternalEvent;
                InternalEvent = null;
                return handle;
            });
        }

        /// <returns>Returns true if subscription was successful and false if handle needs to be invoked immediately.</returns>
        private bool Subscribe(EventHandle handle)
        {
            return m_lock.ExecuteRead(() =>
            {
                if (!m_invoked)
                    InternalEvent += handle;    // multiple readers can increment this simultaniously because the operation is threadsafe

                return !m_invoked;
            });
        }
    }
}
