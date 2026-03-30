using System;
using System.Collections.Generic;
using System.Linq;
using BlockAndDagger.Core;
using UnityEngine;

namespace BlockAndDagger.Services
{
    [Serializable]
    internal class _StringArrayWrapper
    {
        public string[] Items;
    }

    public sealed class RewardService
    {
        private const string PrefsKey = "RewardService.UnlockedTileTypes";
        private readonly IAddressablesManager _addressablesManager;
        public bool HasNewRewardsAvailable { get; private set; }

        public RewardService(IAddressablesManager addressablesManager)
        {
            _addressablesManager = addressablesManager;
        }
        
        public void CheckRewards(LevelName passedLevel)
        {
            if(passedLevel == LevelName.Level_2)
            {
                UnlockRewards(new List<TileType>() { TileType.Rocks });
                Debug.Log("Got a reward!: Rock Block");
            }
        }

        public void UnlockRewards(List<TileType> tileTypes)
        {
            var set = new HashSet<string>();
            if (tileTypes != null)
            {
                foreach (var t in tileTypes)
                {
                    set.Add(t.ToString().ToLower());
                }
            }

            var wrapper = new _StringArrayWrapper { Items = set.ToArray() };
            var json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
            HasNewRewardsAvailable = true;
        }

        public List<ManifestEntry> LoadGainedRewards()
        {
            var json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return new List<ManifestEntry>();
            }

            var wrapper = JsonUtility.FromJson<_StringArrayWrapper>(json);
            if (wrapper == null || wrapper.Items == null)
            {
                return new List<ManifestEntry>();
            }

            var tileTypes = MapToAddressableGroups(wrapper.Items.ToList());
            return tileTypes;
        }

        public List<ManifestEntry> MapToAddressableGroups(List<string> distinctTileTypes)
        {
            var tileTypesEnum = distinctTileTypes.Select(t => (TileType)Enum.Parse(typeof(TileType), t)).ToList();
            var userTileTypes = _addressablesManager.GetNeededAddressGroups(tileTypesEnum);
            return userTileTypes;
        }
        
        public List<ManifestEntry> MapToAddressableGroups(List<TileType> distinctTileTypes)
        {
            var userTileTypes = _addressablesManager.GetNeededAddressGroups(distinctTileTypes);
            return userTileTypes;
        }
        
    }
}
