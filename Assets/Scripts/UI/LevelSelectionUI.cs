using System.Collections.Generic;
using System.Linq;
using BlockAndDagger.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAndDagger
{
    public sealed class LevelSelectionUI : MonoBehaviour
    {
        [SerializeField] Image m_bigPictureImage; //optional way to present a map
        [SerializeField] private Button m_selectButton;
        [SerializeField] private Button m_backButton;
        [SerializeField] private Transform m_smallPictureScrollBarContent;
        [SerializeField] private LevelAndBlueprint _currentSelection;
        [SerializeField] private Button m_toggleBlueprintUpButton;
        [SerializeField] private Button m_toggleBlueprintDownButton;
        [SerializeField] private TextMeshProUGUI m_blueprintCount;
        [SerializeField] private TextMeshProUGUI m_challengeDescriptionText;

        private List<LevelListItem> _levelScrollList = new();
        private LevelAndBlueprint[] _availableLevels;
        private LevelAndBlueprint[] _availableBlueprints;
        private int _currentBlueprintIndex;
        public LevelAndBlueprint CurrentSelection => _currentSelection;

        private void OnEnable()
        {
            m_selectButton.onClick.AddListener(OnSelectButtonClicked);
            m_backButton.onClick.AddListener(OnBackToMainMenuButtonClicked);
            m_toggleBlueprintUpButton.onClick.AddListener(() => ChangeBlueprint(true));
            m_toggleBlueprintDownButton.onClick.AddListener(() => ChangeBlueprint(false));
        }

        private void OnDisable()
        {
            m_selectButton.onClick.RemoveListener(OnSelectButtonClicked);
            m_backButton.onClick.RemoveListener(OnBackToMainMenuButtonClicked);
            m_toggleBlueprintUpButton.onClick.RemoveListener(() => ChangeBlueprint(true));
            m_toggleBlueprintDownButton.onClick.RemoveListener(() => ChangeBlueprint(false));

            _levelScrollList.ForEach(x => Destroy(x.gameObject));
            _levelScrollList.Clear();
        }

        private void OnSelectButtonClicked()
        {
            GameManager.Instance.RunLevelMakerAndCreateLevel(_currentSelection);
        }
        
        private async void OnBackToMainMenuButtonClicked()
        {
            GameManager.Instance.AudioManager.Unloader.UnloadAll();
            GameManager.Instance.Game?.StopGame();
            GameManager.Instance.DestroyActiveLevelObject();
            _ = await AddressablesManager.LoadSceneAsync("mainmenu_scene");
        }
        
        public void InitScrollContent(LevelName? levelName)
        {
            _availableLevels =  GameManager.Instance.LevelMaker.LevelLoader.GetAllBlueprints();
            AppendList(_availableLevels);

            LevelAndBlueprint levelAndBluePrint = default;
            if (levelName is null)
            {
                 levelAndBluePrint = _availableLevels.First();
            }
            else
            {
                levelAndBluePrint = _availableLevels.First(x => x.Level == levelName && string.IsNullOrWhiteSpace(x.BlueprintName));
            }
            
            ChangeLevel(levelAndBluePrint);
            
            //TODO: solve unwanted y change to use clamped:
            //here https://answers.unity.com/questions/1235692/rect-transform-position-keeps-changing-value-rando.html
        }

        ///There can be thousands of levels
        private void AppendList(LevelAndBlueprint[] availableLevels)
        {
            //Add more dynamically
            foreach (var levelAndBlueprint in availableLevels)
            {
                //TODO: uncomment when enough levels
                /*if (m_smallPictureList.Any(x => x.name == levelAndBlue.Level.ToString()))
                {
                    continue;
                }*/
                
                if(!string.IsNullOrWhiteSpace(levelAndBlueprint.BlueprintName))
                {
                    continue; //List only base levels
                }
                
                GameObject buttonObject = Instantiate(GameManager.Instance.PrefabManager.m_levelIconItemPrefab,
                    transform, false);
                var item = buttonObject.GetComponent<LevelListItem>(); //.Init(levelAndBlue);
                item.Init(levelAndBlueprint, m_smallPictureScrollBarContent,
                    (levelAndBlueprint) =>
                    {
                        if (_currentSelection.Equals(levelAndBlueprint))
                        {
                            return;
                        }
                        
                        ChangeLevel(levelAndBlueprint);
                    });

                _levelScrollList.Add(item);
            }
        }

        private void ChangeLevel(LevelAndBlueprint levelNameAndBlueprint, bool refreshBluePrintList = true)
        {
            _currentSelection = levelNameAndBlueprint;
            if (refreshBluePrintList)
            {
                _availableBlueprints = _availableLevels.Where(x => x.Level == levelNameAndBlueprint.Level).ToArray();
                m_blueprintCount.text = (_availableBlueprints.Length -1).ToString(); //dont count the base level
                _currentBlueprintIndex = 0;
            }
            
            m_challengeDescriptionText.text = _currentSelection.ChallengeDescription;
            
            Camera.main.transform.SetPositionAndRotation(levelNameAndBlueprint.CameraPos,
                Quaternion.Euler(levelNameAndBlueprint.CameraRot));
            
            StartCoroutine(Utils.AsyncUtilities.WaitForTask(GameManager.Instance.LevelMaker.LoadLevelToLevelMaker(levelNameAndBlueprint)));
            //OR just use image
            //m_bigPictureImage.sprite = m_smallPictureList.First(x => x.name == levelNameAndBluePrint.Level.ToString()).transform.Find("Image").GetComponent<Image>().sprite;
        }
        
        private void ChangeBlueprint(bool toggleUp)
        {
            if (toggleUp)
            {
                if ( _currentBlueprintIndex < _availableBlueprints.Length - 1)
                {
                    _currentBlueprintIndex++;
                    ChangeLevel(_availableBlueprints[_currentBlueprintIndex], false);
                }
            }
            else
            {
                if (_currentBlueprintIndex > 0)
                {
                    _currentBlueprintIndex--;
                    ChangeLevel(_availableBlueprints[_currentBlueprintIndex], false);
                }
            }
        }
    }
}