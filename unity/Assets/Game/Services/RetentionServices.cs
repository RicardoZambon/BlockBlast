#nullable enable
using System;
using System.Collections;

namespace BlockPuzzle.Game
{
    /// <summary>The stores' rating prompt (master plan 3.7): Google's In-App Review API and Apple's StoreKit prompt.</summary>
    public interface IReviewPrompt
    {
        void Request();
    }

    /// <summary>The Daily reminder: a local notification, with no server.</summary>
    public interface IReminders
    {
        /// <summary>Asks the system, as a coroutine App runs; done gets whether reminders may show.</summary>
        IEnumerator RequestPermission(Action<bool> done);

        /// <summary>Replaces any scheduled reminder with one at fireUtc.</summary>
        void Schedule(DateTime fireUtc, string title, string body);
    }

    /// <summary>Until Google's In-App Review plugin and StoreKit's prompt are added: does nothing.</summary>
    public sealed class StandInReviewPrompt : IReviewPrompt
    {
        public void Request()
        {
        }
    }

    /// <summary>Where there are no notifications (the Editor, and iOS until it's built): permission is never given.</summary>
    public sealed class StandInReminders : IReminders
    {
        public IEnumerator RequestPermission(Action<bool> done)
        {
            done(false);
            yield break;
        }

        public void Schedule(DateTime fireUtc, string title, string body)
        {
        }
    }
}
