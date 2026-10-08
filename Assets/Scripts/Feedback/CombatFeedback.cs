using System.Collections;
using System.Collections.Generic;
using Relicfall.Combat;
using Relicfall.Player;
using Relicfall.UI;
using Unity.Cinemachine;
using UnityEngine;

namespace Relicfall.Feedback
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulse;
        private ParticleSystem sparks;
        private float lastStop;
        private float lastImpulse;

        private void Awake()
        {
            sparks = GetComponentInChildren<ParticleSystem>();
            if (impulse == null) impulse = GetComponent<CinemachineImpulseSource>();
        }

        private void OnEnable() => Health.OnAnyDamaged += OnDamage;
        private void OnDisable()
        {
            Health.OnAnyDamaged -= OnDamage;
        }

        private void OnDamage(Health target, DamageInfo info)
        {
            bool player = target.GetComponent<PlayerMotor>() != null;
            UIManager.Instance.ShowDamageNumber(target.transform.position + Vector3.up * 0.35f,
                info.Damage, player);
            if (sparks != null)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = info.HitPoint,
                    startColor = player ? new Color(1f, 0.29f, 0.24f) :
                        new Color(1f, 0.76f, 0.38f)
                };
                sparks.Emit(emit, player ? 9 : 6);
            }
            if (impulse != null && Time.unscaledTime - lastImpulse >= 0.20f)
            {
                impulse.GenerateImpulse(player ? 0.11f : 0.045f);
                lastImpulse = Time.unscaledTime;
            }
            if (!player && info.Source != null &&
                info.Source.GetComponent<PlayerCombat>() != null &&
                !GameTime.IsHitStopped && Time.unscaledTime - lastStop > 0.16f)
            {
                lastStop = Time.unscaledTime;
                GameTime.HitStop(0.045f);
            }
        }

#if UNITY_EDITOR
        public void Configure(CinemachineImpulseSource source) => impulse = source;
#endif
    }
}
