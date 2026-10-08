using Relicfall.Combat;
using Relicfall.Player;
using UnityEngine;

namespace Relicfall.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 3.2f;
        [SerializeField, Min(0.1f)] private float lifetime = 2.5f;
        private int direction;
        private int damage;
        private GameObject source;
        private float expires;
        private ProjectilePool pool;

        public void AttachPool(ProjectilePool owner) => pool = owner;

        public void Launch(int heading, int attackDamage, GameObject owner)
        {
            direction = heading;
            damage = attackDamage;
            source = owner;
            expires = Time.time + lifetime;
        }

        private void Update()
        {
            transform.position += Vector3.right * (direction * speed * Time.deltaTime);
            if (Time.time >= expires) Recycle();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerMotor>() is PlayerMotor player)
            {
                var health = player.GetComponent<Health>();
                health.TakeDamage(new DamageInfo(damage, new Vector2(direction * 1.2f, 0.3f),
                    source, transform.position));
                Recycle();
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Ground")) Recycle();
        }

        private void Recycle()
        {
            if (VFXPool.Instance != null) VFXPool.Instance.Play(transform.position);
            if (pool != null) pool.Release(this);
            else Destroy(gameObject);
        }
    }
}
