using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAndDagger
{
    public class LevelListItem : MonoBehaviour
    {
        [SerializeField] private Image m_image;
        [SerializeField] private Image m_lockedImage;
        [SerializeField] private TMP_Text m_levelNumber;

        public void Init(LevelAndBlueprint levelAndBlueprint, Transform scrollContentParent,
            Action<LevelAndBlueprint> ChangeLevelOnClick)
        {
            this.name = levelAndBlueprint.Level.ToString();
            var sprite = Resources.Load<Sprite>(Constants.LevelImagesPath + levelAndBlueprint.Level);
            m_image.sprite = sprite;
            m_image.preserveAspect = true;
            m_lockedImage.enabled = !levelAndBlueprint.Unlocked;
            m_levelNumber.text = levelAndBlueprint.Level.ToString().Replace("Level_","");
            transform.SetParent(scrollContentParent);

            var button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                if (GameManager.Instance.MenuManager.LevelSelectionUI.CurrentSelection.Equals(levelAndBlueprint))
                {
                    return;
                }
                GameManager.Instance.LevelMaker.m_activeLevel.CleanLevel();
                ChangeLevelOnClick.Invoke(levelAndBlueprint);
            });
        }
    }
}