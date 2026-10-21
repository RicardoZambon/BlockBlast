#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>Analytics events, named and shaped as in the master plan's table (3.1).</summary>
    public interface IAnalytics
    {
        /// <summary>The player's choices, for Firebase's consent mode: always before the first event, and again when they change.</summary>
        void SetConsent(ConsentResult consent);

        void Log(string name, IReadOnlyDictionary<string, object>? parameters = null);
    }

    /// <summary>Until Firebase is wired in with your project (3.1): nothing leaves the phone; development builds log the name.</summary>
    public sealed class StandInAnalytics : IAnalytics
    {
        public void SetConsent(ConsentResult consent)
        {
        }

        public void Log(string name, IReadOnlyDictionary<string, object>? parameters = null)
        {
            if (Debug.isDebugBuild) Debug.Log("Analytics event: " + name);
        }
    }

    /// <summary>
    /// Drops every event until the consent flow finishes, then passes them on (3.2: nothing collects data before
    /// consent). Firebase's consent mode then decides what each one may store.
    /// </summary>
    public sealed class ConsentGatedAnalytics : IAnalytics
    {
        readonly IAnalytics _inner;

        public ConsentGatedAnalytics(IAnalytics inner)
        {
            _inner = inner;
        }

        public bool IsOpen { get; private set; }

        /// <summary>Passes on the player's choices first, so consent mode is set before any event arrives.</summary>
        public void Open(ConsentResult consent)
        {
            _inner.SetConsent(consent);
            IsOpen = true;
        }

        public void SetConsent(ConsentResult consent) => _inner.SetConsent(consent);

        public void Log(string name, IReadOnlyDictionary<string, object>? parameters = null)
        {
            if (IsOpen) _inner.Log(name, parameters);
        }
    }
}
