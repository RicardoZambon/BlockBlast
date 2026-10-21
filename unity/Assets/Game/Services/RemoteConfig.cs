#nullable enable
using System;

namespace BlockPuzzle.Game
{
    /// <summary>Firebase Remote Config, behind what the app needs from it (master plan 3.1). RemoteTuning reads the keys.</summary>
    public interface IRemoteConfig
    {
        /// <summary>Fetches and activates the latest values; done runs either way, offline too.</summary>
        void Fetch(Action done);

        /// <summary>A key's active value, or null when Remote Config has none and the bundled default applies.</summary>
        string? Value(string key);
    }

    /// <summary>Until Firebase is wired in with your project (3.1): no values, so the bundled defaults apply.</summary>
    public sealed class StandInRemoteConfig : IRemoteConfig
    {
        public void Fetch(Action done) => done();

        public string? Value(string key) => null;
    }
}
