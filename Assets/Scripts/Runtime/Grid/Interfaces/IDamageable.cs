using System.Threading;
using UnityEngine;

public interface IDamageable
{
    public Awaitable TakeDamage(float dmg, CancellationToken token);
}
