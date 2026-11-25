using System;
using UnityEngine;

namespace Game.Home
{
    /// <summary>
    /// Texts and layout of the start screen (feature-home/home-screen/home-screen-spec).
    /// Positions are px on the 1920x1080 reference screen, measured from the top-left corner.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Home/Home Config")]
    internal sealed class HomeConfig : ScriptableObject
    {
        [SerializeField] private string title = "Thủ thành anh hùng";
        [Tooltip("{0} is the best score in waves.")]
        [SerializeField] private string recordFormat = "Kỉ lục: {0} đợt";
        [SerializeField] private string playLabel = "Chơi";
        [Tooltip("Best score shown when none was saved yet.")]
        [SerializeField] private int recordWhenNone = 0;
        [Tooltip("Taps on Play that count each time the screen shows.")]
        [SerializeField] private int tapsPerShow = 1;

        [SerializeField] private Vector2 referenceScreen = new Vector2(1920, 1080);
        [SerializeField] private Rect titleRect = new Rect(360, 200, 1200, 160);
        [SerializeField] private Rect recordRect = new Rect(660, 420, 600, 80);
        [SerializeField] private Rect playRect = new Rect(760, 620, 400, 120);

        public string Title => title;
        public string RecordFormat => recordFormat;
        public string PlayLabel => playLabel;
        public int RecordWhenNone => recordWhenNone;
        public int TapsPerShow => tapsPerShow;
        public Vector2 ReferenceScreen => referenceScreen;
        public Rect TitleRect => titleRect;
        public Rect RecordRect => recordRect;
        public Rect PlayRect => playRect;

        /// <summary>Throws, naming the field, when a text is empty or a number cannot work.</summary>
        public void Check()
        {
            if (string.IsNullOrEmpty(title)) throw new InvalidOperationException($"Home config '{name}': title is empty.");
            if (string.IsNullOrEmpty(recordFormat) || !recordFormat.Contains("{0}"))
                throw new InvalidOperationException($"Home config '{name}': recordFormat must contain {{0}}.");
            if (string.IsNullOrEmpty(playLabel)) throw new InvalidOperationException($"Home config '{name}': playLabel is empty.");
            if (recordWhenNone < 0) throw new InvalidOperationException($"Home config '{name}': recordWhenNone is negative.");
            if (tapsPerShow < 1) throw new InvalidOperationException($"Home config '{name}': tapsPerShow must be at least 1.");
            if (referenceScreen.x <= 0 || referenceScreen.y <= 0)
                throw new InvalidOperationException($"Home config '{name}': referenceScreen must be positive.");
        }
    }
}
