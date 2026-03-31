using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace BlockAndDagger
{
    public class SnapScrollItem : MonoBehaviour
    {
        public Image Image;
        public TMP_Text Text;
        public Button Button;

        public static event Action OnTileChanged;

        public void Init(TileType tileType, MultiscrollController multiscrollController, float imageSize = 100f)
        {
            name = tileType.ToString();
            var sprite = Resources.Load<Sprite>(Constants.BlockIconPath + tileType);
            Image.sprite = sprite;
            Image.rectTransform.sizeDelta = new Vector2(imageSize, imageSize);

            int count = Random.Range(1, 10); //TODO: class/struct for ScrollSnapItem
            Text.text = "x" + count;
            Button.onClick.AddListener(() =>
            {
                multiscrollController.SelectedBlock = tileType;
                OnTileChanged?.Invoke();
            });
        }
    }
}
