using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Dev button: switches RU/EN on the fly.</summary>
    [RequireComponent(typeof(Button))]
    public class LanguageToggle : MonoBehaviour
    {
        public Text label;

        void Start()
        {
            GetComponent<Button>().onClick.AddListener(Loc.Toggle);
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() { Loc.Changed -= Refresh; }

        void Refresh()
        {
            if (label != null) label.text = Loc.Current == Lang.RU ? "RU → EN" : "EN → RU";
        }
    }
}
