using UnityEngine;

public class Bullet : MonoBehaviour
{
    public LayerMask collisionMask;
    public float speed = 10f;
    public float damage = 1f;
    public float lifetime = 3f;
    public float skinWidth = 0.1f;

    private void Start()
    {
        Destroy(gameObject, lifetime);

        Collider[] initialCollisions = Physics.OverlapSphere(transform.position, skinWidth, collisionMask);
        if (initialCollisions.Length > 0)
        {
            OnHitObject(initialCollisions[0], transform.position);
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    private void Update()
    {
        float moveDistance = speed * Time.deltaTime;
        CheckCollisions(moveDistance);
        transform.Translate(Vector3.forward * moveDistance);
    }

    private void CheckCollisions(float moveDistance)
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, moveDistance + skinWidth, collisionMask, QueryTriggerInteraction.Collide))
        {
            OnHitObject(hit.collider, hit.point);
        }
    }

    private void OnHitObject(Collider c, Vector3 hitPoint)
    {
        if (c.TryGetComponent(out Damageable damageableObject))
        {
            damageableObject.TakeHit(damage, hitPoint, transform.forward);
        }
        Destroy(gameObject);
    }
}