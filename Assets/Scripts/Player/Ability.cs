using UnityEngine;

namespace BlockAndDagger
{
    public class Ability
    {
        private readonly float _cooldown;
        private float _jumpTime;
        private float _abilityTimer;
        private bool _timerStarted = false;
        public bool _allowAbility;
        
        public bool HasLimitUsage { get; }
        public PlayerAbility PlayerAbility { get; private set; }

        public Ability(PlayerAbility playerAbility, bool hasLimitUsage = false, float cooldown = 0.0f)
        {
            PlayerAbility = playerAbility;
            _allowAbility = true;
            HasLimitUsage = hasLimitUsage;
            _cooldown = cooldown;
        }
        
        public void UpdateAbilityLimiter(bool groundedPlayer)
        {
            if (!HasLimitUsage)
            {
                return;
            }

            if (!_allowAbility && !_timerStarted && groundedPlayer)
            {
                _timerStarted = true;
                _abilityTimer = 0;
            }
            
            if (_timerStarted)
            {
                _abilityTimer += Time.deltaTime;
                if (_abilityTimer > _cooldown)
                {
                    _allowAbility = true;
                    _timerStarted = false;
                }
            }
        }
    }
}