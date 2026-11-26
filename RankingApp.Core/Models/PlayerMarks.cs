using System.Text.RegularExpressions;

namespace RankingApp.Core.Models
{
    /// <summary>How a player is highlighted in event results. Me wins over Friend.</summary>
    public enum PlayerMark { None, Friend, Me }

    /// <summary>
    /// Finds the app player and the friends among event players. Player ids of the tournament API differ from the
    /// local ones, so players are matched by KeyName (normalized name + surname).
    /// </summary>
    public sealed partial class PlayerMarks
    {
        private readonly HashSet<string> _me = [];
        private readonly HashSet<string> _friends = [];

        public static PlayerMarks Empty { get; } = new([], []);

        public PlayerMarks(IEnumerable<string?> meKeys, IEnumerable<string?> friendKeys)
        {
            foreach (var key in meKeys)
                AddKey(_me, key);

            foreach (var key in friendKeys)
                AddKey(_friends, key);
        }

        public bool IsEmpty => _me.Count == 0 && _friends.Count == 0;

        public PlayerMark Of(string? name, string? surname) => OfKey(NameNormalizer.NormalizeKey($"{name}{surname}"));

        public PlayerMark Of(APIGamePlayer? player) => player == null ? PlayerMark.None : Of(player.Name, player.Surname);

        /// <summary>A game side: the player, or for doubles the pair "Name Surname / Name Surname" (strongest mark of the two).</summary>
        public PlayerMark Of(APIGamePlayer? player, APIDoublesPair? pair)
        {
            if (player != null)
                return Of(player);

            var mark = PlayerMark.None;

            foreach (var part in (pair?.Name ?? "").Split('/'))
            {
                var m = OfKey(NameNormalizer.NormalizeKey(part));
                if (m > mark)
                    mark = m;
            }

            return mark;
        }

        public PlayerMark OfKey(string? key)
        {
            if (string.IsNullOrEmpty(key))
                return PlayerMark.None;

            string plain = StripNotes(key);

            if (_me.Contains(key) || _me.Contains(plain))
                return PlayerMark.Me;

            if (_friends.Contains(key) || _friends.Contains(plain))
                return PlayerMark.Friend;

            return PlayerMark.None;
        }

        private static void AddKey(HashSet<string> set, string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            set.Add(key);
            set.Add(StripNotes(key));   // "edgars(r)berzins" is "Edgars Berzins" in the tournament API
        }

        private static string StripNotes(string key) => NotesRegex().Replace(key, "");

        [GeneratedRegex(@"\([^)]*\)")]
        private static partial Regex NotesRegex();
    }
}
