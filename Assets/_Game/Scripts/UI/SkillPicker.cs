using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>"Level up" overlay with up to 3 skill cards. Built by SceneBuilder.</summary>
    public class SkillPicker : MonoBehaviour
    {
        public Text title;
        public Button[] cards;
        public Image[] cardImages;
        public Text[] cardNames;
        public Text[] cardLevels;
        public Text[] cardDescs;

        List<SkillDef> options;
        Action<SkillDef> onPick;
        float appear;

        void Awake()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].onClick.AddListener(() => Pick(index));
            }
            gameObject.SetActive(false);
        }

        public void Show(List<SkillDef> opts, SkillSystem system, Action<SkillDef> callback)
        {
            options = opts;
            onPick = callback;
            title.text = Loc.F("skill.title", system.Level);
            for (int i = 0; i < cards.Length; i++)
            {
                bool on = i < opts.Count;
                cards[i].gameObject.SetActive(on);
                if (!on) continue;
                var s = opts[i];
                int cur = system.Stars(s);
                int next = cur + 1;
                bool awaken = s.awakenAt > 0 && next == s.awakenAt;
                cardNames[i].text = s.Name;
                cardLevels[i].text = awaken ? Loc.T("skill.awaken") : cur == 0 ? Loc.T("skill.new") : Loc.F("skill.stars", cur, next);
                cardLevels[i].color = awaken ? new Color(1f, 0.35f, 0.2f) : new Color(1f, 0.9f, 0.5f);
                cardDescs[i].text = s.Desc(next);
                Color bg = awaken ? new Color(0.45f, 0.12f, 0.08f) : Color.Lerp(new Color(0.12f, 0.12f, 0.16f), s.color, 0.25f);
                cardImages[i].color = bg;
            }
            appear = 0f;
            gameObject.SetActive(true);
        }

        void Update()
        {
            appear = Mathf.MoveTowards(appear, 1f, Time.unscaledDeltaTime * 5f);
            float s = 0.85f + 0.15f * (1f - (1f - appear) * (1f - appear));
            for (int i = 0; i < cards.Length; i++) cards[i].transform.localScale = Vector3.one * s;
        }

        void Pick(int index)
        {
            if (options == null || index >= options.Count) return;
            var s = options[index];
            options = null;
            gameObject.SetActive(false);
            onPick?.Invoke(s);
        }
    }
}
