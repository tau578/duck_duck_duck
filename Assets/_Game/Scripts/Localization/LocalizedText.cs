using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Static UI label that re-reads its text from Loc when the language changes.</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string key;
        Text text;

        void Awake() { text = GetComponent<Text>(); }
        void OnEnable() { Loc.Changed += Apply; Apply(); }
        void OnDisable() { Loc.Changed -= Apply; }
        void Apply() { if (text != null && !string.IsNullOrEmpty(key)) text.text = Loc.T(key); }
    }
}
